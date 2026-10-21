using ICY.Models;
using ICY.Native;

namespace ICY.Services;

/// <summary>
/// Builds a per-monitor gamma LUT. Deviations from 1.0 are boosted so the
/// sliders produce a visible change. Saturation is extra midtone punch on the
/// same 1D LUT (SetDeviceGammaRamp cannot do a true hue matrix per monitor).
/// </summary>
internal static class ColorMath
{
    public static GammaRamp BuildRamp(
        double brightness,
        double contrast,
        double gamma,
        double saturation,
        ColourVisionMode vision = ColourVisionMode.Normal)
    {
        var red = new ushort[256];
        var green = new ushort[256];
        var blue = new ushort[256];
        VisionScale(vision, out double redScale, out double greenScale, out double blueScale);

        for (int i = 0; i < 256; i++)
        {
            double tone = EvaluateTone(i / 255.0, brightness, contrast, gamma, saturation);
            red[i] = ToRampWord(tone * redScale);
            green[i] = ToRampWord(tone * greenScale);
            blue[i] = ToRampWord(tone * blueScale);
        }

        return new GammaRamp { Red = red, Green = green, Blue = blue };
    }

    public static ushort ToRampValue(double normalized, double brightness, double contrast, double gamma, double saturation)
        => ToRampWord(EvaluateTone(normalized, brightness, contrast, gamma, saturation));

    private static double EvaluateTone(double normalized, double brightness, double contrast, double gamma, double saturation)
    {
        double x = Math.Clamp(normalized, 0.0, 1.0);

        // Brightness: lift + gain so small moves are obvious.
        double bDelta = brightness - 1.0;
        x = x * (1.0 + bDelta * 0.85) + (bDelta * 0.28);

        // Contrast: expand around mid grey, amplified.
        double c = 1.0 + (contrast - 1.0) * 2.15;
        x = (x - 0.5) * c + 0.5;

        // Vibrance: extra S-curve. 1.0 is neutral.
        double s = 1.0 + (saturation - 1.0) * 2.0;
        x = 0.5 + (x - 0.5) * s;
        double curved = x < 0.5
            ? 2.0 * x * x
            : 1.0 - Math.Pow(-2.0 * x + 2.0, 2.0) / 2.0;
        x = x + (curved - x) * Math.Clamp((s - 1.0) * 0.45, -0.85, 0.85);

        x = Math.Clamp(x, 0.0, 1.0);

        double g = gamma <= 1.0
            ? 1.0 - (1.0 - Math.Max(0.12, gamma)) * 1.55
            : 1.0 + (gamma - 1.0) * 1.85;
        g = Math.Max(0.12, g);
        x = Math.Pow(x, 1.0 / g);
        x = Math.Clamp(x, 0.0, 1.0);

        return x;
    }

    private static ushort ToRampWord(double normalized)
        => (ushort)Math.Round(Math.Clamp(normalized, 0.0, 1.0) * 65535.0);

    private static void VisionScale(
        ColourVisionMode vision,
        out double red,
        out double green,
        out double blue)
    {
        switch (vision)
        {
            case ColourVisionMode.GreenVisible:
                red = 0.86;
                green = 1.20;
                blue = 1.04;
                break;
            case ColourVisionMode.RedWeak:
                red = 1.22;
                green = 0.84;
                blue = 1.00;
                break;
            case ColourVisionMode.BlueWeak:
                red = 0.94;
                green = 0.90;
                blue = 1.24;
                break;
            default:
                red = 1.0;
                green = 1.0;
                blue = 1.0;
                break;
        }
    }

    public static GammaRamp CloneRamp(GammaRamp source)
    {
        return new GammaRamp
        {
            Red = (ushort[])source.Red.Clone(),
            Green = (ushort[])source.Green.Clone(),
            Blue = (ushort[])source.Blue.Clone()
        };
    }

    public static GammaRamp IdentityRamp()
    {
        var red = new ushort[256];
        var green = new ushort[256];
        var blue = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            ushort value = (ushort)(i * 257);
            red[i] = value;
            green[i] = value;
            blue[i] = value;
        }

        return new GammaRamp { Red = red, Green = green, Blue = blue };
    }
}
