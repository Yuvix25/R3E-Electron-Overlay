using R3E.Data;
using ReHUD.Models;
using ReHUD.Models.LapData;

namespace ReHUD.Interfaces;

public interface IDriverService {
    public Driver? NewLap(R3EExtraData extraData, DriverData driverData);
    public R3EExtraData ProcessExtraData(R3EExtraData extraData);
    public void UpdateBestLap(Lap? lap);
}