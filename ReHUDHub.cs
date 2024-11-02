using log4net;
using Microsoft.AspNetCore.SignalR;
using ReHUD;
using ReHUD.Interfaces;

namespace SignalRChat.Hubs
{
    public class ReHUDHub : Hub
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ReHUDHub));
        public void Log(string level, double startTimestamp, double endTimestamp, string message) {
            try {
                if (startTimestamp != -1) {
                    startTimestamp /= 1000;
                }
                if (endTimestamp != -1) {
                    endTimestamp /= 1000;
                }

                LogMessage logMessage = new(startTimestamp, endTimestamp, message);
                switch (level) {
                    case "WARN":
                        logger.Warn(logMessage);
                        break;
                    case "ERROR":
                        logger.Error(logMessage);
                        break;
                    default:
                        logger.Info(logMessage);
                        break;
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }


        private IR3EDataService? GetR3EDataService()
        {
            return Context.GetHttpContext()?.RequestServices.GetService<IR3EDataService>();
        }

        public void SaveBestLap(int lapId, double[] points, double pointsPerMeter)
        {
            logger.InfoFormat("SaveBestLap: lapId={0}, points={1}, pointsPerMeter={2}", lapId, points.Length, pointsPerMeter);
            var r3eDataService = GetR3EDataService();
            if (r3eDataService == null) {
                logger.Error("SaveBestLap: r3eDataService is null");
                return;
            }

            try {
                r3eDataService.SaveBestLap(lapId, points, pointsPerMeter);
            } catch (Exception e) {
                logger.Error("SaveBestLap: Failed to save best lap", e);
            }
        }

        public string LoadBestLap()
        {
            logger.InfoFormat("LoadBestLap Invoked");
            var r3eDataService = GetR3EDataService();
            if (r3eDataService == null) {
                logger.Error("LoadBestLap: r3eDataService is null");
                return "{}";
            }

            try {
                return r3eDataService.LoadBestLap();
            } catch (Exception e) {
                logger.Error("LoadBestLap: Failed to load best lap", e);
                return "{}";
            }
        }
    }
}