using log4net;
using Microsoft.AspNetCore.SignalR;
using ReHUD;

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
    }
}