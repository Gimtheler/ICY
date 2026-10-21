using ICY.Models;
using ICY.Native;

namespace ICY.Services;

/// <summary>
/// Per-display gamma LUT via SetDeviceGammaRamp.
/// Only the selected device is written; other captured devices are restored
/// so tweaks do not stay on every screen.
/// </summary>
public sealed class ColorRampService
{
    private readonly Dictionary<string, GammaRamp> _original = new(StringComparer.OrdinalIgnoreCase);

    public void CaptureOriginal(string deviceName)
    {
        if (_original.ContainsKey(deviceName))
        {
            return;
        }

        if (TryGetRamp(deviceName, false, out var ramp))
        {
            _original[deviceName] = ColorMath.CloneRamp(ramp);
        }
        else
        {
            _original[deviceName] = ColorMath.IdentityRamp();
        }
    }

    public bool Apply(
        string deviceName,
        bool isPrimary,
        double brightness,
        double contrast,
        double gamma,
        double saturation,
        ColourVisionMode vision)
    {
        CaptureOriginal(deviceName);
        RestoreOthers(deviceName);
        var ramp = ColorMath.BuildRamp(brightness, contrast, gamma, saturation, vision);
        return SetRamp(deviceName, isPrimary, ref ramp);
    }

    public bool RestoreOriginal(string deviceName, bool isPrimary)
    {
        if (!_original.TryGetValue(deviceName, out var ramp))
        {
            ramp = ColorMath.IdentityRamp();
        }

        var copy = ColorMath.CloneRamp(ramp);
        return SetRamp(deviceName, isPrimary, ref copy);
    }

    public void RestoreOthers(string keepDeviceName)
    {
        foreach (string device in _original.Keys.ToList())
        {
            if (string.Equals(device, keepDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RestoreOriginal(device, isPrimary: false);
        }
    }

    public void RestoreAll()
    {
        foreach (string device in _original.Keys.ToList())
        {
            RestoreOriginal(device, isPrimary: false);
        }
    }

    private static bool TryGetRamp(string deviceName, bool isPrimary, out GammaRamp ramp)
    {
        ramp = ColorMath.IdentityRamp();
        var dc = OpenDisplayDc(deviceName, isPrimary, out bool releaseUserDc);
        if (dc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return NativeMethods.GetDeviceGammaRamp(dc, ref ramp);
        }
        finally
        {
            CloseDc(dc, releaseUserDc);
        }
    }

    private static bool SetRamp(string deviceName, bool isPrimary, ref GammaRamp ramp)
    {
        var dc = OpenDisplayDc(deviceName, isPrimary, out bool releaseUserDc);
        if (dc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (NativeMethods.SetDeviceGammaRamp(dc, ref ramp))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            CloseDc(dc, releaseUserDc);
        }
    }

    private static IntPtr OpenDisplayDc(string deviceName, bool isPrimary, out bool releaseUserDc)
    {
        releaseUserDc = false;
        IntPtr dc = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (dc != IntPtr.Zero)
        {
            return dc;
        }

        dc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);
        if (dc != IntPtr.Zero)
        {
            return dc;
        }

        if (isPrimary)
        {
            dc = NativeMethods.GetDC(IntPtr.Zero);
            releaseUserDc = dc != IntPtr.Zero;
        }

        return dc;
    }

    private static void CloseDc(IntPtr dc, bool releaseUserDc)
    {
        if (dc == IntPtr.Zero)
        {
            return;
        }

        if (releaseUserDc)
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, dc);
        }
        else
        {
            NativeMethods.DeleteDC(dc);
        }
    }
}
