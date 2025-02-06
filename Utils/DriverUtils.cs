using R3E.Data;

namespace ReHUD.Utils;

public static class DriverUtils {
    public static string GetDriverUid(DriverInfo driver) {
        return $"{GetDriverName(driver)}_{driver.userId}_{driver.slotId}_{driver.liveryId}";
    }

    public static string GetDriverName(DriverInfo driver) {
        return System.Text.Encoding.UTF8.GetString(driver.name.TakeWhile(c => c != 0).ToArray());
    }

    public static double CalculateDistanceToDriverAhead(double trackLength, DriverData driver, DriverData driverAhead) {
        double distance = driverAhead.lapDistance - driver.lapDistance;

        if (distance < 0) {
            distance += trackLength;
        }

        return distance;
    }

    public static double CalculateDistanceToDriverBehind(double trackLength, DriverData driver, DriverData driverBehind) {
        return CalculateDistanceToDriverAhead(trackLength, driverBehind, driver);
    }
}