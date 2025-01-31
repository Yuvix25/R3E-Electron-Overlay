using log4net;
using ReHUD.Interfaces;

namespace ReHUD.UpgradeActions;

public class MoveDataFromJsonToSQLite : UpgradeAction
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(MoveDataFromJsonToSQLite));

    public override string Description => "Move data from JSON to SQLite (remove old JSON files)";
    public override Version Version => Version.Parse("0.11.0");

    private static void DeleteFile(string name) {
        try {
            File.Delete(Path.Combine(IUserData.dataPath, name));
        } catch (DirectoryNotFoundException) {
            logger.InfoFormat("File {0} not found during upgrade", name);
        } catch (Exception e) {
            logger.Error($"Failed to delete {name} during upgrade", e);
        }
    }

    public override void Upgrade()
    {
        DeleteFile("lapPointsData.json");
        DeleteFile("userData.json");
    }
}