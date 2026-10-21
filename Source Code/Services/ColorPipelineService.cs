using ICY.Models;

namespace ICY.Services;

/// <summary>
/// Color on the selected monitor only. Magnification fullscreen color is not
/// used — that API paints the whole desktop.
/// </summary>
public sealed class ColorPipelineService : IDisposable
{
    private readonly ColorRampService _ramps = new();
    private readonly ActivityLog _log;

    public ColorPipelineService(ActivityLog log)
    {
        _log = log;
    }

    public void Start()
    {
    }

    public ColorApplyResult Apply(MonitorDevice? monitor, ColorAdjustment color, bool log = false)
    {
        if (monitor is null)
        {
            return new ColorApplyResult(false, "Select a monitor first.");
        }

        _ramps.CaptureOriginal(monitor.DeviceName);
        _ramps.RestoreOthers(monitor.DeviceName);

        bool ok = color.IsDefault
            ? _ramps.RestoreOriginal(monitor.DeviceName, monitor.IsPrimary)
            : _ramps.Apply(
                monitor.DeviceName,
                monitor.IsPrimary,
                color.Brightness,
                color.Contrast,
                color.Gamma,
                color.Saturation,
                color.ColourVision);

        if (ok && log)
        {
            _log.Info(
                $"Color applied on display {monitor.Number}: brightness={color.Brightness:0.00} contrast={color.Contrast:0.00} gamma={color.Gamma:0.00} saturation={color.Saturation:0.00}");
        }

        return new ColorApplyResult(ok, ok ? "Applied." : "This display rejected the color LUT.");
    }

    public void Restore(MonitorDevice? monitor)
    {
        if (monitor is not null)
        {
            _ramps.RestoreOriginal(monitor.DeviceName, monitor.IsPrimary);
            _log.Info($"Color restored on display {monitor.Number}.");
        }
    }

    public void Dispose()
    {
        _ramps.RestoreAll();
        _log.Info("Color restored.");
    }
}

public readonly record struct ColorApplyResult(bool Applied, string Message);
