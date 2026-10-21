namespace ICY.Models;

/// <summary>
/// A named color snapshot. Apply is always manual.
/// </summary>
public sealed class ColorPreset
{
    public required string Name { get; set; }
    public bool IsBuiltIn { get; set; }
    public double Brightness { get; set; } = ColorAdjustment.DefaultBrightness;
    public double Contrast { get; set; } = ColorAdjustment.DefaultContrast;
    public double Gamma { get; set; } = ColorAdjustment.DefaultGamma;
    public double Saturation { get; set; } = ColorAdjustment.DefaultSaturation;
    public ColourVisionMode ColourVision { get; set; } = ColourVisionMode.Normal;

    public string Summary =>
        $"B {Brightness:0.00}   C {Contrast:0.00}   G {Gamma:0.00}   S {Saturation:0.00}";

    public void ApplyTo(ColorAdjustment target)
    {
        target.Brightness = Brightness;
        target.Contrast = Contrast;
        target.Gamma = Gamma;
        target.Saturation = Saturation;
        target.ColourVision = ColourVision;
    }

    public static ColorPreset FromAdjustment(string name, ColorAdjustment color, bool builtIn = false)
        => new()
        {
            Name = name,
            IsBuiltIn = builtIn,
            Brightness = color.Brightness,
            Contrast = color.Contrast,
            Gamma = color.Gamma,
            Saturation = color.Saturation,
            ColourVision = color.ColourVision
        };
}
