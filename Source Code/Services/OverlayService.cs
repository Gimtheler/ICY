using System.Windows.Interop;
using System.Windows.Threading;
using ICY.Models;
using ICY.Native;
using ICY.Views;

namespace ICY.Services;

/// <summary>
/// Owns ICY's overlay HWND. Hotkey polling only shows or hides this window.
/// No input is sent to other apps.
/// </summary>
public sealed class OverlayService : IDisposable
{
    private OverlayWindow? _window;
    private readonly ActivityLog _log;
    private readonly DispatcherTimer _hotkeyTimer;
    private OverlayOptions? _options;
    private MonitorDevice? _monitor;
    private bool _lastEnabled;
    private bool _toggleOn = true;
    private bool _wasDown;

    public OverlayService(ActivityLog log)
    {
        _log = log;
        _hotkeyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(32) };
        _hotkeyTimer.Tick += (_, _) => PollHotkey();
    }

    public IntPtr WindowHandle
        => _window is null ? IntPtr.Zero : new WindowInteropHelper(_window).Handle;

    public void Apply(OverlayOptions options, MonitorDevice? monitor)
    {
        _options = options;
        _monitor = monitor;

        if (!options.Enabled)
        {
            _hotkeyTimer.Stop();
            _window?.Hide();
            if (_lastEnabled)
            {
                _log.Info("Overlay off.");
                _lastEnabled = false;
            }

            return;
        }

        _window ??= new OverlayWindow();
        _window.ApplyVisuals(options, monitor);

        if (options.HotkeyVk == 0)
        {
            _hotkeyTimer.Stop();
            ShowWindow();
        }
        else
        {
            if (options.HotkeyMode == OverlayHotkeyMode.Hold)
            {
                _toggleOn = false;
            }

            _hotkeyTimer.Start();
            PollHotkey();
        }

        if (!_lastEnabled)
        {
            _log.Info("Overlay on.");
            _lastEnabled = true;
        }
    }

    public void BringToFront()
        => _window?.BringToFront();

    private void PollHotkey()
    {
        if (_options is null || !_options.Enabled || _options.HotkeyVk == 0)
        {
            return;
        }

        bool down = (NativeMethods.GetAsyncKeyState(_options.HotkeyVk) & 0x8000) != 0;
        bool show;
        if (_options.HotkeyMode == OverlayHotkeyMode.Hold)
        {
            show = down;
        }
        else
        {
            if (down && !_wasDown)
            {
                _toggleOn = !_toggleOn;
            }

            show = _toggleOn;
        }

        _wasDown = down;
        if (show)
        {
            ShowWindow();
        }
        else
        {
            _window?.Hide();
        }
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
            if (_options is not null)
            {
                _window.ApplyVisuals(_options, _monitor);
            }
        }

        _window.BringToFront();
    }

    public void Dispose()
    {
        _hotkeyTimer.Stop();
        if (_window is null)
        {
            return;
        }

        _window.Close();
        _window = null;
    }
}
