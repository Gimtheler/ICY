using ICY.Infrastructure;

namespace ICY.Models;

/// <summary>
/// Global color parameters applied to a monitor (gamma ramp) and optionally
/// to the desktop compositor (saturation matrix). These values are never
/// bound to a process name or game.
/// </summary>
public sealed class ColorAdjustment : ObservableObject
{
    public const double DefaultBrightness = 1.0;
    public const double DefaultContrast = 1.0;
    public const double DefaultGamma = 1.0;
    public const double DefaultSaturation = 1.0;

    private double _brightness = DefaultBrightness;
    private double _contrast = DefaultContrast;
    private double _gamma = DefaultGamma;
    private double _saturation = DefaultSaturation;
    private ColourVisionMode _colourVision = ColourVisionMode.Normal;

    public double Brightness
    {
        get => _brightness;
        set => SetClamped(ref _brightness, value, 0.20, 2.50);
    }

    public double Contrast
    {
        get => _contrast;
        set => SetClamped(ref _contrast, value, 0.20, 3.00);
    }

    public double Gamma
    {
        get => _gamma;
        set => SetClamped(ref _gamma, value, 0.20, 3.50);
    }

    public double Saturation
    {
        get => _saturation;
        set => SetClamped(ref _saturation, value, 0.00, 3.00);
    }

    public ColourVisionMode ColourVision
    {
        get => _colourVision;
        set
        {
            if (SetField(ref _colourVision, value))
            {
                OnPropertyChanged(nameof(IsVisionNormal));
                OnPropertyChanged(nameof(IsVisionGreenVisible));
                OnPropertyChanged(nameof(IsVisionRedWeak));
                OnPropertyChanged(nameof(IsVisionBlueWeak));
            }
        }
    }

    public bool IsVisionNormal => ColourVision == ColourVisionMode.Normal;
    public bool IsVisionGreenVisible => ColourVision == ColourVisionMode.GreenVisible;
    public bool IsVisionRedWeak => ColourVision == ColourVisionMode.RedWeak;
    public bool IsVisionBlueWeak => ColourVision == ColourVisionMode.BlueWeak;

    private bool SetClamped(ref double field, double value, double min, double max, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        double next = Math.Clamp(value, min, max);
        if (Math.Abs(field - next) < 0.0005)
        {
            return false;
        }

        field = next;
        OnPropertyChanged(name);
        return true;
    }

    public bool IsRampNeutral =>
        Math.Abs(Brightness - DefaultBrightness) < 0.001
        && Math.Abs(Contrast - DefaultContrast) < 0.001
        && Math.Abs(Gamma - DefaultGamma) < 0.001;

    public bool IsSaturationNeutral => Math.Abs(Saturation - DefaultSaturation) < 0.001;

    public bool IsDefault =>
        IsRampNeutral && IsSaturationNeutral && ColourVision == ColourVisionMode.Normal;

    public void CopyFrom(ColorAdjustment other)
    {
        Brightness = other.Brightness;
        Contrast = other.Contrast;
        Gamma = other.Gamma;
        Saturation = other.Saturation;
        ColourVision = other.ColourVision;
    }

    public ColorAdjustment Clone()
    {
        var copy = new ColorAdjustment();
        copy.CopyFrom(this);
        return copy;
    }

    public void ResetToNeutral()
    {
        Brightness = DefaultBrightness;
        Contrast = DefaultContrast;
        Gamma = DefaultGamma;
        Saturation = DefaultSaturation;
        ColourVision = ColourVisionMode.Normal;
    }
}
