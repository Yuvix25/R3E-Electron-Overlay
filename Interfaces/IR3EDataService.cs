using ElectronNET.API;
using ReHUD.Models;
using ReHUD.Models.LapData;

namespace ReHUD.Interfaces
{
    public interface IR3EDataService : IDisposable
    {
        public R3EExtraData Data { get; }

        public BrowserWindow HUDWindow { get; set; }
        public bool? HUDShown { get; set; }
        public string[]? UsedKeys { get; set; }

        public void SetEnteredEditMode();
        public Task SendEmptyData();

        public void SaveBestLap(Lap lap, double[] points, int pointsGap);
        public Lap? LoadBestLap();
    }
}
