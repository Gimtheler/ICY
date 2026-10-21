using System.Windows;
using System.Windows.Threading;
using ICY.Models;
using ICY.Native;
using ICY.Views;

namespace ICY.Services;

/// <summary>
/// ICY's own Magnifier-lens window. Hotkey polling only shows or hides it.
/// No input is sent to other apps.
/// </summary>
public sealed class ZoomService : IDisposable
{
    private readonly ActivityLog _log;
    private readonly DispatcherTimer _hotkeyTimer;
    private readonly DispatcherTimer _refreshTimer;
    private ZoomWindow? _window;
    private ZoomOptions? _options;
    private MonitorDevice? _monitor;
    private double _offsetX;
    private double _offsetY;
    private Func<IEnumerable<IntPtr>>? _exclude;
    private Action? _bringOverlayToFront;
    private bool _ready;
    private bool _lastEnabled;
    private bool _toggleOn = true;
    private bool _wasDown;
    private bool _visible;

    public ZoomService(ActivityLog log)
    {
        _log = log;
        _hotkeyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(32) };
        _hotkeyTimer.Tick += (_, _) => PollHotkey();
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _refreshTimer.Tick += (_, _) => RefreshSource();
    }

    public void Apply(
        ZoomOptions options,
        MonitorDevice? monitor,
        double offsetX,
        double offsetY,
        Func<IEnumerable<IntPtr>>? exclude = null,
        Action? bringOverlayToFront = null)
    {
        _options = options;
        _monitor = monitor;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _exclude = exclude;
        _bringOverlayToFront = bringOverlayToFront;

        if (!options.Enabled)
        {
            _hotkeyTimer.Stop();
            _refreshTimer.Stop();
            HideWindow();
            if (_lastEnabled)
            {
                _log.Info("Zoom off.");
                _lastEnabled = false;
            }

            return;
        }

        if (!EnsureReady())
        {
            return;
        }

        _refreshTimer.Interval = TimeSpan.FromMilliseconds(options.UpdateMilliseconds);

        if (options.HotkeyVk == 0)
        {
            _hotkeyTimer.Stop();
            ShowWindow(layout: true);
        }
        else
        {
            if (options.HotkeyMode == OverlayHotkeyMode.Hold)
            {
                _toggleOn = false;
            }

            _hotkeyTimer.Start();
            PollHotkey();
            if (_visible)
            {
                ShowWindow(layout: true);
            }
        }

        if (!_lastEnabled)
        {
            _log.Info("Zoom on.");
            _lastEnabled = true;
        }
    }

    public void Dispose()
    {
        _hotkeyTimer.Stop();
        _refreshTimer.Stop();
        if (_window is not null)
        {
            _window.Close();
            _window = null;
        }

        if (_ready)
        {
            NativeMethods.MagUninitialize();
            _ready = false;
        }
    }

    private bool EnsureReady()
    {
        if (_ready)
        {
            return true;
        }

        if (!NativeMethods.MagInitialize())
        {
            _log.Info("Zoom unavailable: Magnifier API failed to start.");
            return false;
        }

        NativeMethods.MagShowSystemCursor(true);
        _ready = true;
        return true;
    }

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
            ShowWindow(layout: false);
        }
        else
        {
            HideWindow();
        }
    }

    private void ShowWindow(bool layout)
    {
        if (_options is null)
        {
            return;
        }

        _window ??= new ZoomWindow();
        if (!_window.IsVisible)
        {
            _window.Show();
            layout = true;
        }

        if (layout)
        {
            _window.ApplyVisuals(_options, _monitor, _offsetX, _offsetY, CurrentExclude());
        }

        _visible = true;
        _bringOverlayToFront?.Invoke();
        if (!_refreshTimer.IsEnabled)
        {
            _refreshTimer.Start();
        }
    }

    private void HideWindow()
    {
        _refreshTimer.Stop();
        _visible = false;
        _window?.Hide();
    }

    private void RefreshSource()
    {
        if (!_visible || _window is null || _options is null)
        {
            return;
        }

        _window.RefreshSource(_options, _monitor, _offsetX, _offsetY);
    }

    private IEnumerable<IntPtr> CurrentExclude()
    {
        if (Application.Current?.MainWindow is Window main)
        {
            yield return new System.Windows.Interop.WindowInteropHelper(main).Handle;
        }

        if (_exclude is null)
        {
            yield break;
        }

        foreach (IntPtr hwnd in _exclude())
        {
            yield return hwnd;
        }
    }
}
