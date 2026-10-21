using System.Runtime.InteropServices;

namespace ICY.Native;

/// <summary>
/// P/Invoke surface for ICY.
///
/// SAFETY: Every entry here is a documented user-mode Windows API that talks to
/// the display driver / Desktop Window Manager — never to another process.
///
/// This file intentionally does NOT declare or call:
///   OpenProcess, EnumProcesses, ReadProcessMemory, WriteProcessMemory,
///   CreateRemoteThread, NtOpenProcess, NtReadVirtualMemory, NtWriteVirtualMemory,
///   SetWindowsHookEx, SendInput, mouse_event, keybd_event,
///   or any DirectX / OpenGL / Vulkan hook or injection API.
/// </summary>
internal static class NativeMethods
{
    // -------------------------------------------------------------------------
    // Display enumeration and mode changes (user32)
    // Used only to list monitors / timings and to change the OS desktop mode.
    // These APIs are monitor-scoped. They are not per-application and do not
    // inspect window titles or process names.
    // -------------------------------------------------------------------------

    public const int EnumCurrentSettings = -1;
    public const uint DisplayDeviceAttachedToDesktop = 0x00000001;
    public const uint DisplayDevicePrimaryDevice = 0x00000004;
    public const uint DisplayDeviceMirroringDriver = 0x00000008;

    public const uint DmPelsWidth = 0x00080000;
    public const uint DmPelsHeight = 0x00100000;
    public const uint DmBitsPerPel = 0x00040000;
    public const uint DmDisplayFrequency = 0x00400000;
    public const uint DmPosition = 0x00000020;

    public const uint CdsUpdateRegistry = 0x00000001;
    public const uint CdsTest = 0x00000002;
    public const uint CdsReset = 0x40000000;

    public const int DispChangeSuccessful = 0;
    public const int DispChangeRestart = 1;
    public const int DispChangeFailed = -1;
    public const int DispChangeBadMode = -2;
    public const int DispChangeNotUpdated = -3;
    public const int DispChangeBadFlags = -4;
    public const int DispChangeBadParam = -5;
    public const int DispChangeBadDualView = -6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayDevices(
        string? deviceName,
        uint deviceNumber,
        ref DisplayDevice displayDevice,
        uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsExW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplaySettingsEx(
        string? deviceName,
        int modeNumber,
        ref DevMode devMode,
        uint flags);

    /// <summary>
    /// Changes the desktop mode of a monitor. CDS_UPDATEREGISTRY writes the
    /// user's display settings at the OS level (same path as Settings &gt; System &gt; Display).
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
    public static extern int ChangeDisplaySettingsEx(
        string? deviceName,
        ref DevMode devMode,
        IntPtr hwnd,
        uint flags,
        IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr clipRect,
        MonitorEnumProc callback,
        IntPtr extra);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr extra);

    // -------------------------------------------------------------------------
    // Per-monitor gamma ramps (gdi32)
    // SetDeviceGammaRamp / GetDeviceGammaRamp adjust the hardware LUT for a
    // display device. This is a global display property, identical in class to
    // the ramps used by accessibility / night-light style tools.
    // It does not patch drivers and does not hook any application's renderer.
    // -------------------------------------------------------------------------

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateDCW")]
    public static extern IntPtr CreateDC(
        string? driver,
        string? device,
        string? output,
        IntPtr initData);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetDeviceGammaRamp(IntPtr hdc, ref GammaRamp ramp);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDeviceGammaRamp(IntPtr hdc, ref GammaRamp ramp);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    /// <summary>
    /// Reads keyboard/mouse button state for ICY's own overlay hotkey.
    /// Does not send input and does not install a hook into other processes.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int virtualKey);

    // -------------------------------------------------------------------------
    // Magnification (Magnification.dll)
    // Same family as the OS Magnifier lens. Used for ICY's own zoom HWND and
    // (unused) fullscreen colour matrix. Does not attach to other processes.
    // -------------------------------------------------------------------------

    public const string WcMagnifier = "Magnifier";
    public const int MwFilterModeExclude = 0;
    public const int WsChild = 0x40000000;
    public const int WsVisible = 0x10000000;
    public const int WsClipSiblings = 0x04000000;
    public const int WsClipChildren = 0x02000000;

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagInitialize();

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagUninitialize();

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetFullscreenColorEffect(ref MagColorEffect effect);

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowSource(IntPtr hwnd, Rect rect);

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowTransform(IntPtr hwnd, ref MagTransform transform);

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowFilterList(
        IntPtr hwnd,
        int filterMode,
        int count,
        IntPtr[] hwnds);

    [DllImport("Magnification.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagShowSystemCursor([MarshalAs(UnmanagedType.Bool)] bool show);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
    public static extern IntPtr CreateWindowEx(
        int exStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr param);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? module);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateEllipticRgn(int x1, int y1, int x2, int y2);

    [DllImport("user32.dll")]
    public static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, [MarshalAs(UnmanagedType.Bool)] bool redraw);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(
        IntPtr hwnd,
        int x,
        int y,
        int width,
        int height,
        [MarshalAs(UnmanagedType.Bool)] bool repaint);

    // -------------------------------------------------------------------------
    // Overlay window styles (user32)
    // Applied only to ICY's own top-level HWND so DWM can composite a layered,
    // click-through window. WS_EX_TRANSPARENT means the overlay never receives
    // mouse input — ICY does not intercept or synthesize player input.
    // -------------------------------------------------------------------------

    public const int GwlExStyle = -20;
    public const int WsExTransparent = 0x00000020;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExLayered = 0x00080000;
    public const int WsExNoActivate = 0x08000000;
    public const int WsExTopmost = 0x00000008;

    public const uint SwpNoActivate = 0x0010;
    public const uint SwpNoMove = 0x0002;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpShowWindow = 0x0040;
    public const uint SwpNoOwnerZOrder = 0x0200;
    public const uint SwpNoCopyBits = 0x0100;

    public static readonly IntPtr HwndTopmost = new(-1);
    public static readonly IntPtr HwndTop = IntPtr.Zero;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr hwnd, int index, int newLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr newLong);

    public static int GetWindowLong(IntPtr hwnd, int index)
        => IntPtr.Size == 8
            ? unchecked((int)GetWindowLongPtr64(hwnd, index).ToInt64())
            : GetWindowLong32(hwnd, index);

    public static int SetWindowLong(IntPtr hwnd, int index, int newLong)
        => IntPtr.Size == 8
            ? unchecked((int)SetWindowLongPtr64(hwnd, index, new IntPtr(newLong)).ToInt64())
            : SetWindowLong32(hwnd, index, newLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    // Tray icon for ICY's own process only. Not a hook and not input injection.
    public const uint NimAdd = 0;
    public const uint NimModify = 1;
    public const uint NimDelete = 2;
    public const uint NifMessage = 0x00000001;
    public const uint NifIcon = 0x00000002;
    public const uint NifTip = 0x00000004;
    public const int TrayCallbackMessage = 0x0400 + 1;
    public const int WmLButtonUp = 0x0202;
    public const int WmLButtonDblClk = 0x0203;
    public const int WmRButtonUp = 0x0205;
    public const uint MfString = 0x00000000;
    public const uint TpmRightButton = 0x0002;
    public const uint TpmReturnCmd = 0x0100;
    public const uint WmNull = 0;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int ExtractIconEx(
        string file,
        int iconIndex,
        IntPtr[]? largeIcons,
        IntPtr[]? smallIcons,
        int icons);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AppendMenu(IntPtr menu, uint flags, uint id, string text);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out TrayPoint point);

    [DllImport("user32.dll")]
    public static extern uint TrackPopupMenu(
        IntPtr menu,
        uint flags,
        int x,
        int y,
        int reserved,
        IntPtr hwnd,
        IntPtr rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("Shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    public const int MdtEffectiveDpi = 0;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DisplayDevice
{
    public int cb;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;
    public uint StateFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceId;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DevMode
{
    private const int DeviceNameLength = 32;
    private const int FormNameLength = 32;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DeviceNameLength)]
    public string dmDeviceName;
    public short dmSpecVersion;
    public short dmDriverVersion;
    public short dmSize;
    public short dmDriverExtra;
    public int dmFields;
    public int dmPositionX;
    public int dmPositionY;
    public int dmDisplayOrientation;
    public int dmDisplayFixedOutput;
    public short dmColor;
    public short dmDuplex;
    public short dmYResolution;
    public short dmTTOption;
    public short dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FormNameLength)]
    public string dmFormName;
    public short dmLogPixels;
    public int dmBitsPerPel;
    public int dmPelsWidth;
    public int dmPelsHeight;
    public int dmDisplayFlags;
    public int dmDisplayFrequency;
    public int dmICMMethod;
    public int dmICMIntent;
    public int dmMediaType;
    public int dmDitherType;
    public int dmReserved1;
    public int dmReserved2;
    public int dmPanningWidth;
    public int dmPanningHeight;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GammaRamp
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Red;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Green;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Blue;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MagColorEffect
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    public float[] Transform;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MagTransform
{
    public float M00;
    public float M01;
    public float M02;
    public float M10;
    public float M11;
    public float M12;
    public float M20;
    public float M21;
    public float M22;

    public static MagTransform Scale(float factor)
        => new()
        {
            M00 = factor,
            M11 = factor,
            M22 = 1f
        };
}

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TrayPoint
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NotifyIconData
{
    public int Size;
    public IntPtr Hwnd;
    public uint Id;
    public uint Flags;
    public uint CallbackMessage;
    public IntPtr Icon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Tip;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MonitorInfoEx
{
    public int cbSize;
    public Rect rcMonitor;
    public Rect rcWork;
    public uint dwFlags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string szDevice;
}
