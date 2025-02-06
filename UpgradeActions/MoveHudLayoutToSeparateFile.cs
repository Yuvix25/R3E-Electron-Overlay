using log4net;
using Newtonsoft.Json.Linq;

namespace ReHUD.UpgradeActions;

public class MoveHudLayoutToSeparateFile : UpgradeAction
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(MoveHudLayoutToSeparateFile));

    public override string Description => "Move HUD layout to separate file";
    public override Version Version => Version.Parse("0.8.0");

    public override void Upgrade()
    {
        try {
            Startup.settings.Load();
            HudLayout.LoadHudLayouts();
            var hudLayout = (JObject?) Startup.settings.Data.Remove("hudLayout") ?? (!HudLayout.Layouts.Any() ? new JObject() : null);
            if (hudLayout != null) {
                logger.Info("Found HUD layout in settings, moving to separate file");

                HudLayout hudLayoutData = HudLayout.AddHudLayout(new(true));

                foreach (var element in hudLayout) {
                    if (element.Value is JArray elementData) {
                        var id = element.Key;
                        var left = elementData[0]?.Value<double?>() ?? 0;
                        var top = elementData[1]?.Value<double?>() ?? 0;
                        var scale = elementData[2]?.Value<double>() ?? 1;
                        var shown = elementData[3]?.Value<bool>() ?? true;
                        hudLayoutData.AddElement(id, left, top, scale, shown);
                    }
                }
                hudLayoutData.Save();

                Startup.settings.Save();
            }
        } catch (Exception e) {
            logger.Error("Failed to move HUD layout to separate file", e);
        }
    }
}