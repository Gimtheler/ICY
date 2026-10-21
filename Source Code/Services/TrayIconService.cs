using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ICY.Native;

namespace ICY.Services;

public sealed class TrayIconService : IDisposable
{
    private const uint OpenCommand = 1;
    private const uint ExitCommand = 2;

    private HwndSource? _source;
    private IntPtr _icon;
    private NotifyIconData _data;
    private bool _visible;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    public bool Visible
    {
        get => _visible;
        set
        {
            EnsureCreated();
            if (_visible == value || _source is null)
            {
                return;
            }

            _visible = value;
            NativeMethods.Shell_NotifyIcon(
                value ? NativeMethods.NimAdd : NativeMethods.NimDelete,
                ref _data);
        }
    }

    public void Dispose()
    {
        if (_visible)
        {
            NativeMethods.Shell_NotifyIcon(NativeMethods.NimDelete, ref _data);
            _visible = false;
        }

        if (_icon != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }

        _source?.Dispose();
        _source = null;
    }

    private void EnsureCreated()
    {
        if (_source is not null)
        {
            return;
        }

        _source = new HwndSource(new HwndSourceParameters("ICY tray")
        {
            ParentWindow = new IntPtr(-3),
            Width = 0,
            Height = 0
        });
        _source.AddHook(WndProc);
        _icon = LoadIcon();

        _data = new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            Hwnd = _source.Handle,
            Id = 1,
            Flags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip,
            CallbackMessage = NativeMethods.TrayCallbackMessage,
            Icon = _icon,
            Tip = "ICY"
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.TrayCallbackMessage)
        {
            return IntPtr.Zero;
        }

        int mouse = lParam.ToInt32() & 0xFFFF;
        if (mouse is NativeMethods.WmLButtonUp or NativeMethods.WmLButtonDblClk)
        {
            OpenRequested?.Invoke();
            handled = true;
        }
        else if (mouse == NativeMethods.WmRButtonUp)
        {
            ShowMenu(hwnd);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ShowMenu(IntPtr hwnd)
    {
        IntPtr menu = NativeMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, OpenCommand, "Open");
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, ExitCommand, "Exit");
            NativeMethods.GetCursorPos(out TrayPoint point);
            NativeMethods.SetForegroundWindow(hwnd);
            uint command = NativeMethods.TrackPopupMenu(
                menu,
                NativeMethods.TpmRightButton | NativeMethods.TpmReturnCmd,
                point.X,
                point.Y,
                0,
                hwnd,
                IntPtr.Zero);
            NativeMethods.PostMessage(hwnd, NativeMethods.WmNull, IntPtr.Zero, IntPtr.Zero);

            if (command == OpenCommand)
            {
                OpenRequested?.Invoke();
            }
            else if (command == ExitCommand)
            {
                ExitRequested?.Invoke();
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    private static IntPtr LoadIcon()
    {
        string? path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "ICY.exe");
        }

        if (!File.Exists(path))
        {
            return IntPtr.Zero;
        }

        var small = new IntPtr[1];
        if (NativeMethods.ExtractIconEx(path, 0, null, small, 1) > 0)
        {
            return small[0];
        }

        return IntPtr.Zero;
    }
}
