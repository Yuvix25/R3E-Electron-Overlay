namespace ReHUD.UpgradeActions;

public abstract class UpgradeAction {
    public abstract Version Version { get; }
    public abstract string Description { get; }

    public bool IsApplicable(Version oldVersion, Version newVersion) {
        return Version.CompareTo(oldVersion) > 0 && Version.CompareTo(newVersion) <= 0;
    }

    public abstract void Upgrade();
}
