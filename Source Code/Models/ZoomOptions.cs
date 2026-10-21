using ICY.Infrastructure;

namespace ICY.Models;

public enum ZoomQuality
{
    UltraQuality,
    Quality,
    Balanced,
    Performance,
    UltraPerformance
}

public enum ZoomOutline
{
    None,
    White,
    Black
}

public sealed class ZoomOptions : ObservableObject
{
    private bool _enabled;
    private double _factor = 2.5;
    private double _size = 220;
    private double _outlineThickness = 4;
    private ZoomOutline _outline = ZoomOutline.White;
    private ZoomQuality _quality = ZoomQuality.Balanced;
    private string? _targetDeviceName;
    private int _hotkeyVk;
    private OverlayHotkeyMode _hotkeyMode = OverlayHotkeyMode.Hold;

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public double Factor
    {
        get => _factor;
        set => SetClamped(ref _factor, value, 1.5, 8.0, 1);
    }

    public double Size
    {
        get => _size;
        set => SetClamped(ref _size, value, 80, 520, 0);
    }

    public double OutlineThickness
    {
        get => _outlineThickness;
        set => SetClamped(ref _outlineThickness, value, 1, 16, 0);
    }

    public ZoomOutline Outline
    {
        get => _outline;
        set => SetField(ref _outline, value);
    }

    public ZoomQuality Quality
    {
        get => _quality;
        set => SetField(ref _quality, value);
    }

    public string? TargetDeviceName
    {
        get => _targetDeviceName;
        set => SetField(ref _targetDeviceName, value);
    }

    public int HotkeyVk
    {
        get => _hotkeyVk;
        set
        {
            if (SetField(ref _hotkeyVk, value))
            {
                OnPropertyChanged(nameof(HotkeyLabel));
            }
        }
    }

    public OverlayHotkeyMode HotkeyMode
    {
        get => _hotkeyMode;
        set => SetField(ref _hotkeyMode, value);
    }

    public string HotkeyLabel => HotkeyVk == 0 ? "None" : HotkeyNames.Describe(HotkeyVk);

    public int UpdateMilliseconds => Quality switch
    {
        ZoomQuality.UltraQuality => 8,
        ZoomQuality.Quality => 12,
        ZoomQuality.Balanced => 16,
        ZoomQuality.Performance => 28,
        ZoomQuality.UltraPerformance => 40,
        _ => 16
    };

    public int OutlinePixels => Outline == ZoomOutline.None ? 0 : (int)Math.Round(OutlineThickness);

    public void CopyFrom(ZoomOptions other)
    {
        Enabled = other.Enabled;
        Factor = other.Factor;
        Size = other.Size;
        OutlineThickness = other.OutlineThickness;
        Outline = other.Outline;
        Quality = other.Quality;
        TargetDeviceName = other.TargetDeviceName;
        HotkeyVk = other.HotkeyVk;
        HotkeyMode = other.HotkeyMode;
    }

    public ZoomOptions Clone()
    {
        var copy = new ZoomOptions();
        copy.CopyFrom(this);
        return copy;
    }

    private bool SetClamped(
        ref double field,
        double value,
        double min,
        double max,
        int decimals,
        [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        double next = Math.Round(Math.Clamp(value, min, max), decimals);
        if (Math.Abs(field - next) < 0.0005)
        {
            return false;
        }

        field = next;
        OnPropertyChanged(name);
        return true;
    }
}
