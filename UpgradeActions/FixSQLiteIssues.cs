using log4net;
using ReHUD.Interfaces;
using ReHUD.Models.LapData;
using ReHUD.Services;

namespace ReHUD.UpgradeActions;

public class FixSQLiteIssues : UpgradeAction
{
    public override string Description => "Fix SQLite issues (remove old DB files)";
    public override Version Version => Version.Parse("0.11.1");

    public override void Upgrade()
    {
        LapDataContext context = new();
        context.Database.EnsureDeleted();

        // Initialize the LapDataService to create a new database.
        _ = new LapDataService();
    }
}