using log4net;
using ReHUD.Models.LapData;

namespace ReHUD.Models;

public class PositionJumpException : Exception {
    public PositionJumpException(string message) : base(message) { }
}

public class Driver {
    private static readonly ILog logger = LogManager.GetLogger(typeof(Driver));

    /// <summary>
    /// Minimum laptime in seconds to prevent some weird values.
    /// </summary>
    private static readonly int MIN_LAPTIME = 10;

    /// <summary>
    /// Distance in meters between data points.
    /// </summary>
    public static readonly int DATA_POINTS_GAP = 1;

    /// <summary>
    /// Minimum distance in meters to consider a position jump.
    /// </summary>
    private static readonly int POSITION_JUMP_THRESHOLD = 150;

    /// <summary>
    /// Minimum distance in meters to consider a negative progress.
    /// </summary>
    private static readonly int NEGATIVE_PROGRESS_THRESHOLD = 30;

    private static readonly double EPSILON = 0.005;

    private readonly string uid;
    private readonly double trackLength;

    private DataPoints dataPoints;
    public DataPoints? BestLap {get; private set;} = null;
    private DataPoints sessionBestLap = null;

    private bool currentLapValid = true;
    private bool bestLapValid = true;

    private double? bestLapTime = null;
    private double? sessionBestLapTime = null;

    private double? crossedFinishLineTime = null;
    private bool attemptedLoadingBestLap = false;
    private bool lapEnded = false;

    private static Driver? mainDriver = null;

    public Driver(string uid, double trackLength, int completedLaps) {
        this.uid = uid;
        this.trackLength = trackLength;
        this.dataPoints = new DataPoints(trackLength, DATA_POINTS_GAP);
    }

    public void ClearTempData() {
        crossedFinishLineTime = null;
        SetLapInvalid();
        dataPoints = new DataPoints(trackLength, DATA_POINTS_GAP);
    }

    public void SetLapInvalid() {
        currentLapValid = false;
    }


    public bool IsMainDriver() {
        return this == mainDriver;
    }

    public static Driver? GetMainDriver() {
        return mainDriver;
    }

    /// <summary>
    /// Set the main driver.
    /// </summary>
    /// <param name="driver">The new main driver.</param>
    /// <returns>True if we need to load the best lap, false otherwise.</returns>
    public static bool SetMainDriver(Driver driver) {
        if (driver != mainDriver) {
            logger.InfoFormat("Setting main driver to {0}", driver.uid);
        }

        mainDriver = driver;

        if (!driver.attemptedLoadingBestLap && (driver.BestLap == null || !driver.bestLapValid)) {
            driver.attemptedLoadingBestLap = true;

            return true;
        }

        return false;
    }

    public bool SetAsMainDriver() {
        return SetMainDriver(this);
    }

    public void LoadBestLap(double bestLaptime, DataPoints points, int pointsGap) {
        logger.InfoFormat("Loading best lap for {0}. Laptime: {1}, Points: {2}, Gap: {3}", uid, bestLaptime, points.Size(), pointsGap);
        if (pointsGap == DATA_POINTS_GAP) {
            BestLap = points;
        } else {
            DataPoints newPoints = new(trackLength, pointsGap);
            newPoints.SetIndex(-1);

            if (pointsGap < DATA_POINTS_GAP) {
                for (int i = 0; i < newPoints.Size(); i++) {
                    double index = i * DATA_POINTS_GAP / (double) pointsGap;
                    newPoints.AddDataPoint(points.GetDataPoint(index));
                }
            } else {
                for (int i = 0; i < newPoints.Size(); i++) {
                    double index = i * pointsGap / (double) DATA_POINTS_GAP;
                    newPoints.AddDataPoint(points.GetDataPoint(index));
                }
            }

            BestLap = newPoints;
        }

        bestLapTime = bestLaptime;
        bestLapValid = true;
    }

    public void SetBestLap(Lap lap) {
        LoadBestLap(lap.LapTime.Value, new DataPoints(lap.Telemetry!.Value.Points), lap.Telemetry!.Value.PointsGap);
    }


    /// <summary>
    /// Add a new data point.
    /// </summary>
    /// <param name="distance">Current distance.</param>
    /// <param name="timeNow">Current game simulation time. (Seconds)</param>
    /// <returns>True if we encountered a position jump, false otherwise.</returns>
    /// <exception cref="PositionJumpException"></exception>
    public bool AddDataPoint(double distance, double timeNow) {
        try {
            int newIndex = (int) Math.Floor(distance / DATA_POINTS_GAP);

            if (!dataPoints.IsIndexSet()) {
                dataPoints.SetIndex(newIndex);
                dataPoints.SetDataPoint(timeNow);
            } else {
                int lastIndex = dataPoints.GetIndex()!.Value;

                if (!lapEnded && newIndex < lastIndex) {
                    if ((lastIndex - newIndex) * DATA_POINTS_GAP > NEGATIVE_PROGRESS_THRESHOLD) {
                        logger.WarnFormat("Negative progress detected for {0}. Gap: {1}, Last index: {2}, New index: {3}", uid, (newIndex - lastIndex) * DATA_POINTS_GAP, lastIndex, newIndex);
                        ClearTempData();

                        return true;
                    } else {
                        return false;
                    }
                }

                int diff = dataPoints.FillDataGap(newIndex, timeNow);

                if (diff > POSITION_JUMP_THRESHOLD / DATA_POINTS_GAP) {
                    return true;
                }
            }

            return false;
        } finally {
            lapEnded = false;
        }
    }

    public bool EndLap(double? laptime, double timeNow, int completedLaps, R3E.Constant.Session session, bool? safeMode) {
        try {
            logger.DebugFormat("Ending lap for {0}. Laptime: {1}, Completed laps: {2}", uid, laptime, completedLaps);

            bool shouldSaveBestLap = false;

            if (dataPoints.GetIndex() == 0) {
                logger.ErrorFormat("Point for new lap added before previous lap ended for {0}.", uid);
                SetLapInvalid();
                return false;
            }

            dataPoints.FillDataGap(-1, timeNow);
            lapEnded = true;

            if (CrossedFinishLine() && (session != R3E.Constant.Session.Race || completedLaps > 1)) {
                if (laptime == null || laptime < 0) {
                    if (Math.Abs(crossedFinishLineTime!.Value - dataPoints.GetDataPoint(0)!.Value) > 2) {
                        logger.WarnFormat("Gap too big between finish line time and first data point for {0}. Crossed finish line time: {1}, First data point: {2}, Time now: {3}", uid, crossedFinishLineTime, dataPoints.GetDataPoint(0), timeNow);
                        SetLapInvalid();
                        return false;
                    }

                    laptime = timeNow - dataPoints.GetDataPoint(0)!.Value;
                }

                if (laptime < MIN_LAPTIME) {
                    logger.WarnFormat("Laptime for {0} is too low. Laptime: {1}", uid, laptime);
                    SetLapInvalid();
                    return false;
                }

                logger.DebugFormat("Safe mode: {0}, Best laptime: {1}, Current lap valid: {2}, Best lap valid: {3}", safeMode, bestLapTime, currentLapValid, bestLapValid);

                if (safeMode == null || !safeMode!.Value) {
                    if (bestLapTime == null || (laptime < bestLapTime && currentLapValid) || (currentLapValid && !bestLapValid)) {
                        logger.InfoFormat("Current lap valid: {0}, Is main driver: {1}", currentLapValid, IsMainDriver());
                        if (currentLapValid && IsMainDriver()) {
                            shouldSaveBestLap = true;
                        }

                        if (shouldSaveBestLap || completedLaps >= 0) {
                            BestLap = dataPoints.CloneAndSubtractFirst();
                            bestLapTime = laptime;
                            bestLapValid = currentLapValid;

                            logger.InfoFormat("New best lap for {0}. Laptime: {1}", uid, laptime);
                        }
                    }

                    if (currentLapValid && (sessionBestLapTime == null || laptime < sessionBestLapTime)) {
                        sessionBestLap = dataPoints.CloneAndSubtractFirst();
                        sessionBestLapTime = laptime;

                        logger.InfoFormat("New session best lap for {0}. Laptime: {1}", uid, laptime);
                    }
                }
            }

            currentLapValid = true;

            return shouldSaveBestLap;
        } finally {
            crossedFinishLineTime = timeNow;
        }
    }

    public double? CalculateDeltaToDriverAhead(Driver driverAhead) {
        if (dataPoints.GetIndex() == null || driverAhead.dataPoints.GetIndex() == null) {
            return null;
        }

        int index = dataPoints.GetIndex()!.Value;
        int aheadIndex = driverAhead.dataPoints.GetIndex()!.Value;

        if (index == aheadIndex) {
            return 0;
        }

        if ((IsMainDriver() || driverAhead.IsMainDriver()) && mainDriver.BestLap != null) {
            double? res = mainDriver.CalculateDeltaBasedOnBestLap(index, aheadIndex);
            if (res != null) {
                return res;
            }
        }

        if (!IsMainDriver()) {
            double? res = CalculateDeltaBasedOnBestLap(index, aheadIndex);
            if (res != null) {
                return res;
            }
        }

        double? byAheadCurrent = driverAhead.CalculateDeltaBasedOnCurrentLap(index, aheadIndex);
        if (byAheadCurrent != null) {
            return byAheadCurrent;
        }

        return driverAhead.CalculateDeltaBasedOnBestLap(index, aheadIndex) ?? CalculateDeltaBasedOnCurrentLap(index + 1, aheadIndex);
    }

    public double? CalculateDeltaToDriverBehind(Driver driverBehind) {
        return driverBehind.CalculateDeltaToDriverAhead(this);
    }

    public double? CalculateDeltaBasedOnBestLap(int index1, int index2) {
        return CalculateDelta(BestLap, index1, index2);
    }

    public double? CalculateDeltaBasedOnCurrentLap(int index1, int index2) {
        return CalculateDelta(dataPoints, index1, index2);
    }

    private double? CalculateDelta(DataPoints points, int indexBehind, int indexAhead) {
        double? res = points.CalculateTimeDifference(indexBehind, indexAhead);

        if (res != null) {
            if (Math.Abs(res.Value) < EPSILON) {
                return 0;
            }

            if (indexAhead < indexBehind) {
                return EstimatedLapTime() - res;
            }

            return res;
        }

        return null;
    }

    public double? CalculateDeltaToBestLap(double distance, double? currentTime) {
        if (BestLap == null || !bestLapValid || currentTime == null) {
            return null;
        }

        double index = distance / DATA_POINTS_GAP;

        return BestLap.CalculateDelta(index, currentTime.Value);
    }

    public double? CalculateDeltaToSessionBestLap(double distance, double? currentTime) {
        if (sessionBestLap == null || currentTime == null) {
            return null;
        }

        double index = distance / DATA_POINTS_GAP;

        return sessionBestLap.CalculateDelta(index, currentTime.Value);
    }

    public double? EstimatedLapTime() {
        if (bestLapTime == null) {
            if (BestLap == null) {
                return null;
            }

            return BestLap.GetDataPoint(BestLap.GetIndex()!.Value) - BestLap.GetDataPoint(0);
        }

        return bestLapTime;
    }

    public double? GetBestLapTime() {
        return bestLapTime;
    }

    public double? GetSessionBestLapTime() {
        return sessionBestLapTime;
    }

    public bool CrossedFinishLine() {
        return crossedFinishLineTime != null;
    }

    public double? GetCurrentLaptime(double timeNow) {
        if (crossedFinishLineTime == null) {
            return null;
        }

        return timeNow - crossedFinishLineTime;
    }

    public override bool Equals(object? obj)
    {
        //
        // See the full list of guidelines at
        //   http://go.microsoft.com/fwlink/?LinkID=85237
        // and also the guidance for operator== at
        //   http://go.microsoft.com/fwlink/?LinkId=85238
        //
        
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        return uid == ((Driver)obj).uid;
    }
    
    public override int GetHashCode()
    {
        return uid.GetHashCode();
    }
}