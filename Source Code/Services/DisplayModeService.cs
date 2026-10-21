using ICY.Models;
using ICY.Native;

namespace ICY.Services;

/// <summary>
/// Enumerates monitors and desktop timings, then applies a mode with
/// ChangeDisplaySettingsEx. This is the same OS path used by Windows Settings.
///
/// SAFETY: No window-title inspection, no process enumeration, no per-app mode.
/// A change applies to the selected display for the whole desktop session.
/// </summary>
public sealed class DisplayModeService
{
    public IReadOnlyList<MonitorDevice> GetMonitors()
    {
        var boundsByDevice = GetMonitorBounds();
        var monitors = new List<MonitorDevice>();

        var adapter = NewDisplayDevice();
        for (uint adapterIndex = 0; EnumDisplayDevices(null, adapterIndex, ref adapter); adapterIndex++)
        {
            if ((adapter.StateFlags & NativeMethods.DisplayDeviceMirroringDriver) != 0)
            {
                adapter = NewDisplayDevice();
                continue;
            }

            if ((adapter.StateFlags & NativeMethods.DisplayDeviceAttachedToDesktop) == 0)
            {
                adapter = NewDisplayDevice();
                continue;
            }

            string adapterName = adapter.DeviceName;
            string adapterFriendly = adapter.DeviceString;
            bool adapterPrimary = (adapter.StateFlags & NativeMethods.DisplayDevicePrimaryDevice) != 0;

            var monitor = NewDisplayDevice();
            bool foundMonitor = false;
            for (uint monitorIndex = 0; EnumDisplayDevices(adapterName, monitorIndex, ref monitor); monitorIndex++)
            {
                if ((monitor.StateFlags & NativeMethods.DisplayDeviceAttachedToDesktop) == 0)
                {
                    monitor = NewDisplayDevice();
                    continue;
                }

                foundMonitor = true;
                boundsByDevice.TryGetValue(adapterName, out var bounds);
                monitors.Add(CreateMonitor(
                    adapterName,
                    adapterFriendly,
                    string.IsNullOrWhiteSpace(monitor.DeviceString) ? adapterFriendly : monitor.DeviceString,
                    adapterPrimary,
                    bounds));
                monitor = NewDisplayDevice();
            }

            if (!foundMonitor)
            {
                boundsByDevice.TryGetValue(adapterName, out var bounds);
                monitors.Add(CreateMonitor(adapterName, adapterFriendly, adapterFriendly, adapterPrimary, bounds));
            }

            adapter = NewDisplayDevice();
        }

        var ordered = monitors
            .OrderBy(m => m.PixelLeft)
            .ThenBy(m => m.PixelTop)
            .ThenBy(m => m.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Number = i + 1;
        }

        return ordered;
    }

    public IReadOnlyList<DisplayModeOption> GetModes(string deviceName, bool includeLowResolutions)
    {
        var unique = new HashSet<DisplayModeOption>();
        var mode = NewDevMode();
        for (int index = 0; NativeMethods.EnumDisplaySettingsEx(deviceName, index, ref mode, 0); index++)
        {
            if (mode.dmPelsWidth <= 0 || mode.dmPelsHeight <= 0 || mode.dmDisplayFrequency <= 0)
            {
                mode = NewDevMode();
                continue;
            }

            if (!includeLowResolutions && (mode.dmPelsWidth < 800 || mode.dmPelsHeight < 600))
            {
                mode = NewDevMode();
                continue;
            }

            unique.Add(new DisplayModeOption
            {
                Width = mode.dmPelsWidth,
                Height = mode.dmPelsHeight,
                RefreshHz = mode.dmDisplayFrequency,
                BitsPerPixel = mode.dmBitsPerPel
            });
            mode = NewDevMode();
        }

        return unique
            .OrderByDescending(m => m.Width)
            .ThenByDescending(m => m.Height)
            .ThenByDescending(m => m.RefreshHz)
            .ThenByDescending(m => m.BitsPerPixel)
            .ToList();
    }

    public DisplayModeOption? GetCurrentMode(string deviceName)
    {
        var mode = NewDevMode();
        if (!NativeMethods.EnumDisplaySettingsEx(deviceName, NativeMethods.EnumCurrentSettings, ref mode, 0))
        {
            return null;
        }

        return new DisplayModeOption
        {
            Width = mode.dmPelsWidth,
            Height = mode.dmPelsHeight,
            RefreshHz = mode.dmDisplayFrequency,
            BitsPerPixel = mode.dmBitsPerPel
        };
    }

    public DisplayChangeResult ApplyMode(string deviceName, DisplayModeOption requested)
    {
        var current = NewDevMode();
        if (!NativeMethods.EnumDisplaySettingsEx(deviceName, NativeMethods.EnumCurrentSettings, ref current, 0))
        {
            return new DisplayChangeResult(false, "Could not read the current desktop mode for this monitor.");
        }

        current.dmPelsWidth = requested.Width;
        current.dmPelsHeight = requested.Height;
        current.dmDisplayFrequency = requested.RefreshHz;
        current.dmBitsPerPel = requested.BitsPerPixel;
        current.dmFields = unchecked((int)(NativeMethods.DmPelsWidth
                                           | NativeMethods.DmPelsHeight
                                           | NativeMethods.DmDisplayFrequency
                                           | NativeMethods.DmBitsPerPel));

        int test = NativeMethods.ChangeDisplaySettingsEx(
            deviceName,
            ref current,
            IntPtr.Zero,
            NativeMethods.CdsTest,
            IntPtr.Zero);

        if (test != NativeMethods.DispChangeSuccessful)
        {
            return new DisplayChangeResult(false, DescribeChangeResult(test));
        }

        int applied = NativeMethods.ChangeDisplaySettingsEx(
            deviceName,
            ref current,
            IntPtr.Zero,
            NativeMethods.CdsUpdateRegistry | NativeMethods.CdsReset,
            IntPtr.Zero);

        bool ok = applied == NativeMethods.DispChangeSuccessful;
        return new DisplayChangeResult(ok, DescribeChangeResult(applied), applied == NativeMethods.DispChangeRestart);
    }

    private static string DescribeChangeResult(int code) => code switch
    {
        NativeMethods.DispChangeSuccessful => "Desktop mode applied.",
        NativeMethods.DispChangeRestart => "Windows needs a restart to finish this mode change.",
        NativeMethods.DispChangeBadMode => "This monitor rejected the selected timing.",
        NativeMethods.DispChangeFailed => "The display driver could not apply the mode.",
        NativeMethods.DispChangeNotUpdated => "Windows could not write the mode to the registry.",
        NativeMethods.DispChangeBadFlags => "The mode-change request used invalid flags.",
        NativeMethods.DispChangeBadParam => "The mode-change request used an invalid parameter.",
        NativeMethods.DispChangeBadDualView => "The mode is not compatible with the current multi-monitor layout.",
        _ => $"Display change returned code {code}."
    };

    private static bool EnumDisplayDevices(string? name, uint index, ref DisplayDevice device)
        => NativeMethods.EnumDisplayDevices(name, index, ref device, 0);

    private static DisplayDevice NewDisplayDevice()
    {
        return new DisplayDevice
        {
            cb = System.Runtime.InteropServices.Marshal.SizeOf<DisplayDevice>(),
            DeviceName = string.Empty,
            DeviceString = string.Empty,
            DeviceId = string.Empty,
            DeviceKey = string.Empty
        };
    }

    private static DevMode NewDevMode()
    {
        return new DevMode
        {
            dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf<DevMode>(),
            dmDeviceName = string.Empty,
            dmFormName = string.Empty
        };
    }

    private static MonitorDevice CreateMonitor(
        string deviceName,
        string adapterName,
        string friendlyName,
        bool isPrimary,
        MonitorBounds bounds)
    {
        return new MonitorDevice
        {
            DeviceName = deviceName,
            AdapterName = adapterName,
            FriendlyName = friendlyName,
            IsPrimary = isPrimary,
            PixelLeft = bounds.Left,
            PixelTop = bounds.Top,
            PixelWidth = bounds.Width,
            PixelHeight = bounds.Height,
            DpiX = bounds.DpiX == 0 ? 96 : bounds.DpiX,
            DpiY = bounds.DpiY == 0 ? 96 : bounds.DpiY
        };
    }

    private static Dictionary<string, MonitorBounds> GetMonitorBounds()
    {
        var map = new Dictionary<string, MonitorBounds>(StringComparer.OrdinalIgnoreCase);
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr _, ref Rect _, IntPtr _) =>
        {
            var info = new MonitorInfoEx
            {
                cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>(),
                szDevice = string.Empty
            };

            if (NativeMethods.GetMonitorInfo(monitor, ref info) && !string.IsNullOrWhiteSpace(info.szDevice))
            {
                uint dpiX = 96;
                uint dpiY = 96;
                _ = NativeMethods.GetDpiForMonitor(monitor, NativeMethods.MdtEffectiveDpi, out dpiX, out dpiY);
                map[info.szDevice] = new MonitorBounds(
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top,
                    dpiX,
                    dpiY);
            }

            return true;
        }, IntPtr.Zero);

        return map;
    }

    private readonly record struct MonitorBounds(int Left, int Top, int Width, int Height, uint DpiX, uint DpiY);
}

public readonly record struct DisplayChangeResult(bool Success, string Message, bool RestartRequired = false);
