namespace ICY.Models;

public sealed class AppSettings
{
    public string? SelectedMonitorDeviceName { get; set; }
    public ColorAdjustment Color { get; set; } = new();
    public OverlayOptions Overlay { get; set; } = new();
    public ZoomOptions Zoom { get; set; } = new();
    public bool ShowAllDisplayModes { get; set; }
    public bool LoggingEnabled { get; set; }
    public bool ConsoleEnabled { get; set; }
    public bool AutomaticStartup { get; set; }
    public bool MinimizeToTrayOnClose { get; set; }
    public bool StartMinimized { get; set; }
}
