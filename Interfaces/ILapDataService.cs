using Microsoft.EntityFrameworkCore.Storage;
using R3E;
using ReHUD.Models.LapData;

namespace ReHUD.Interfaces
{
    public interface ILapDataService
    {
        public static readonly string DATA_PATH = Path.Combine(IUserData.dataPath, "UserData.db");
        public static readonly int MAX_ENTRIES = 20;

        public void SaveChanges();
        public IDbContextTransaction BeginTransaction();

        public T AttachContext<T>(T context) where T : Context;
        public Lap LogLap(LapContext context, bool valid, double lapTime);
        public void Log<T>(T entry) where T : LapPointer;
        public bool RemoveLapPointer<T>(T pointer) where T : LapPointer;

        public void UpdateLapTime(Lap lap, double lapTime);

        public CombinationSummary GetCombinationSummary(int trackLayoutId, int carId, Constant.TireSubtype frontTireCompound, Constant.TireSubtype rearTireCompound);
        public Lap? GetLap(int lapId);
        public Lap? GetCarBestLap(int trackLayoutId, int carId, Constant.TireSubtype frontTireCompound, Constant.TireSubtype rearTireCompound);
        public Lap? GetClassBestLap(int trackLayoutId, int carId, int classPerformanceIndex, Constant.TireSubtype frontTireCompound, Constant.TireSubtype rearTireCompound);
    }
}
