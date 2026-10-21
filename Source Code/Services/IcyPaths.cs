using System.IO;

namespace ICY.Services;

/// <summary>
/// ICY stores only its own settings under %AppData%\ICY.
/// It never searches Steam, Epic, Riot, or other application folders.
/// </summary>
public static class IcyPaths
{
    public static string Root
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "ICY");
        }
    }

    public static string PresetsFile => Path.Combine(Root, "presets.json");
    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string ActivityLogFile => Path.Combine(Root, "activity.log");

    public static void EnsureRoot() => Directory.CreateDirectory(Root);
}
