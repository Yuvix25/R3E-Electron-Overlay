using log4net;
using R3E.Data;
using ReHUD.Interfaces;
using ReHUD.Models;
using ReHUD.Models.LapData;
using ReHUD.Utils;

namespace ReHUD.Services;

public class DriverService : IDriverService, IDisposable
{
    public static readonly ILog logger = LogManager.GetLogger(typeof(DriverService));
    private static readonly int REMOVED_DRIVER_TEMP_DATA_RETENTION_TIME = 3;

    private readonly IEventService eventService;

    private readonly Dictionary<string, Driver> drivers = new();
    private readonly Dictionary<string, Tuple<double, Driver>> removedDrivers = new();

    private int leaderCrossedFinishLineAt0 = 0;

    public DriverService(IEventService eventService) {
        this.eventService = eventService;

        this.eventService.PositionJump += ClearTempData;
        this.eventService.SessionChange += (sender, e) => {
            if (e.NewValue == R3E.Constant.Session.Unavailable) {
                drivers.Clear();
            }

            ClearTempData();
            removedDrivers.Clear();

            leaderCrossedFinishLineAt0 = 0;
        };
        this.eventService.SessionPhaseChange += (sender, e) => {
            if (Utilities.SessionPhaseNotDriving(e.NewValue) || Utilities.SessionPhaseNotDriving(e.OldValue)) {
                ClearTempData();
            }
        };
        this.eventService.EnterPitlane += ClearTempData;
        this.eventService.ExitPitlane += ClearTempData;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }

    public R3EExtraData ProcessExtraData(R3EExtraData extraData) {
        R3EData data = extraData.rawData;

        if (data.gameInReplay == 1 && data.gameInMenus == 0) {
            ClearTempData();
        }

        if (data.gamePaused == 1) {
            return extraData;
        }

        double trackLength = data.layoutLength;
        int phase = data.sessionPhase;

        if (phase < 3) {
            drivers.Clear();

            return extraData;
        }
        
        HashSet<string> existingUids = new();
        List<int> classes = new();

        double timeNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        DriverData? mainDriverData = null;
        foreach (DriverData driver in data.driverData) {
            int classIndex = driver.driverInfo.classPerformanceIndex;
            classes.Add(classIndex);

            if (driver.place == data.position) {
                mainDriverData = driver;
            }

            string uid = DriverUtils.GetDriverUid(driver.driverInfo);

            if (!drivers.ContainsKey(uid)) {
                if (removedDrivers.ContainsKey(uid)) {
                    Tuple<double, Driver> removedDriver = removedDrivers[uid];

                    drivers.Add(uid, removedDriver.Item2);

                    double diff = timeNow - removedDriver.Item1;

                    if (diff < REMOVED_DRIVER_TEMP_DATA_RETENTION_TIME) {
                        logger.DebugFormat("Restoring driver {0} after {1} seconds (temp data in tact)", uid, diff);
                    } else {
                        logger.DebugFormat("Restoring driver {0} after {1} seconds (temp data expired)", uid, diff);
                        removedDriver.Item2.ClearTempData();
                    }

                    removedDriver.Item2.SetLapInvalid();
                    removedDrivers.Remove(uid);
                } else {
                    logger.DebugFormat("Adding driver {0}", uid);
                    drivers.Add(uid, new Driver(DriverUtils.GetDriverUid(driver.driverInfo), trackLength, driver.completedLaps));
                }
            }

            if (driver.currentLapValid == 0) {
                drivers[uid].SetLapInvalid();
            }

            existingUids.Add(uid);
        }

        List<string> uidsToRemove = new();

        foreach (string uid in drivers.Keys) {
            if (!existingUids.Contains(uid)) {
                logger.DebugFormat("Removing driver {0}", uid);

                removedDrivers.Add(uid, new Tuple<double, Driver>(timeNow, drivers[uid]));
                uidsToRemove.Add(uid);
            }
        }

        foreach (string uid in uidsToRemove) {
            drivers.Remove(uid);
        }

        foreach (DriverData driverData in data.driverData) {
            string uid = DriverUtils.GetDriverUid(driverData.driverInfo);

            if (driverData.place == data.position && Driver.GetMainDriver() != drivers[uid]) {
                drivers[uid].SetAsMainDriver();
                eventService.MainDriverChanged(data, driverData);
                break;
            }
        }

        // Populate extra data deltasAhead and deltasBehind
        Dictionary<string, double?> deltasAhead = new();
        Dictionary<string, double?> deltasBehind = new();

        Driver? mainDriver = Driver.GetMainDriver();
        if (mainDriver != null && mainDriverData != null) {
            foreach (DriverData driverData in data.driverData) {
                string uid = DriverUtils.GetDriverUid(driverData.driverInfo);

                if (deltasAhead.ContainsKey(uid) || deltasBehind.ContainsKey(uid)) {
                    continue;
                }

                Driver driver = drivers[uid];

                if (!driver.IsMainDriver()) {
                    double? deltaAhead = mainDriver.CalculateDeltaToDriverAhead(driver);
                    double? deltaBehind = mainDriver.CalculateDeltaToDriverBehind(driver);

                    if (deltaAhead == null && deltaBehind == null) {
                        double distanceAhead = DriverUtils.CalculateDistanceToDriverAhead(trackLength, mainDriverData!.Value, driverData);
                        double distanceBehind = DriverUtils.CalculateDistanceToDriverBehind(trackLength, mainDriverData!.Value, driverData);

                        if (distanceAhead < distanceBehind) {
                            deltasAhead.Add(uid, null);
                        } else {
                            deltasBehind.Add(uid, null);
                        }
                    } else if (deltaAhead == null) {
                        deltasBehind.Add(uid, deltaBehind);
                    } else if (deltaBehind == null) {
                        deltasAhead.Add(uid, -deltaAhead);
                    } else {
                        if (deltaAhead < deltaBehind) {
                            deltasAhead.Add(uid, -deltaAhead);
                        } else {
                            deltasBehind.Add(uid, deltaBehind);
                        }
                    }
                }

                driver.AddDataPoint(driverData.completedLaps, driverData.lapDistance, extraData.rawData.player.gameSimulationTime);
            }

            double? currentLaptime = mainDriver.GetCurrentLaptime(extraData.rawData.player.gameSimulationTime);
            extraData.currentLaptime = currentLaptime;

            extraData.deltaToSessionBestLap = mainDriver.CalculateDeltaToSessionBestLap(mainDriverData!.Value.lapDistance, currentLaptime);
            extraData.deltaToBestLap = mainDriver.CalculateDeltaToBestLap(mainDriverData!.Value.lapDistance, currentLaptime);

            extraData.bestLapTime = mainDriver.GetBestLapTime();
            extraData.sessionBestLapTime = mainDriver.GetSessionBestLapTime();

            extraData.crossedFinishLine = mainDriver.CrossedFinishLine();
        } else {
            extraData.deltaToSessionBestLap = null;
            extraData.deltaToBestLap = null;

            extraData.bestLapTime = null;
            extraData.sessionBestLapTime = null;

            extraData.crossedFinishLine = false;
            extraData.currentLaptime = null;
        }

        extraData.deltasAhead = deltasAhead;
        extraData.deltasBehind = deltasBehind;

        extraData.leaderCrossedFinishLineAt0 = leaderCrossedFinishLineAt0;

        return extraData;
    }

    public void UpdateBestLap(Lap? lap) {
        if (Driver.GetMainDriver() == null) {
            return;
        }

        if (lap?.Telemetry != null) {
            Driver.GetMainDriver()!.SetBestLap(lap);
        }
    }

    private void ClearTempData() {
        foreach (var driver in drivers.Values) {
            driver.ClearTempData();
        }
    }

    private void ClearTempData(object? sender, DriverEventArgs e) {
        string uid = DriverUtils.GetDriverUid(e.Driver.driverInfo);

        if (drivers.ContainsKey(uid)) {
            drivers[uid].ClearTempData();
        }
    }

    public Tuple<Driver, bool>? NewLap(R3EExtraData extraData, DriverData driverData) {
        if (driverData.place == 1 && (extraData.rawData.sessionTimeRemaining == 0 || leaderCrossedFinishLineAt0 > 0)) {
            leaderCrossedFinishLineAt0++;
        }

        string uid = DriverUtils.GetDriverUid(driverData.driverInfo);

        if (drivers.ContainsKey(uid)) {
            Driver driver = drivers[uid];

            bool shouldSaveBestLap = driver.EndLap(extraData.rawData.player.gameSimulationTime, driverData, (R3E.Constant.Session) extraData.rawData.sessionType, (bool?) Startup.settings.Data.Get("relativeSafeMode", false));

            if (shouldSaveBestLap && extraData.rawData.gameInMenus == 0 && extraData.rawData.gameInReplay == 0 && extraData.rawData.gamePaused == 0) {
                return new(driver, true);
            } else {
                return new(driver, false);
            }
        }

        return null;
    }
}