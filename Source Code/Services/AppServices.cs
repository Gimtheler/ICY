using ICY.Models;

namespace ICY.Services;

/// <summary>
/// Composition root. Starts and stops only ICY's own OS-level services.
/// </summary>
public sealed class AppServices : IDisposable
{
    public ActivityLog Log { get; } = new();
    public DisplayModeService Display { get; } = new();
    public PresetStore Presets { get; } = new();
    public SettingsStore Settings { get; } = new();
    public ColorPipelineService Color { get; }
    public OverlayService Overlay { get; }
    public ZoomService Zoom { get; }
    public DisplayIdentifyService Identify { get; } = new();

    public AppSettings CurrentSettings { get; private set; } = new();

    public AppServices()
    {
        Color = new ColorPipelineService(Log);
        Overlay = new OverlayService(Log);
        Zoom = new ZoomService(Log);
    }

    public void Start()
    {
        IcyPaths.EnsureRoot();
        CurrentSettings = Settings.Load();
        Log.LoggingEnabled = CurrentSettings.LoggingEnabled;
        Log.ConsoleEnabled = CurrentSettings.ConsoleEnabled;
        DebugConsole.SetVisible(CurrentSettings.ConsoleEnabled);
        Color.Start();
        Log.Info("ICY started.");
    }

    public void Save(AppSettings settings)
    {
        CurrentSettings = settings;
        try
        {
            Settings.Save(settings);
        }
        catch (Exception ex)
        {
            Log.Info("Could not save settings: " + ex.Message);
        }
    }

    public void Dispose()
    {
        Identify.Hide();
        Overlay.Dispose();
        Zoom.Dispose();
        Color.Dispose();
        Log.Info("ICY exited.");
        DebugConsole.SetVisible(false);
    }
}
