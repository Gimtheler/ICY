using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using System.Windows.Threading;
using ICY.Infrastructure;
using ICY.Models;
using ICY.Services;
using Microsoft.Win32;

namespace ICY.ViewModels;

public enum AppPage
{
    Display,
    Color,
    Overlay,
    Zoom,
    Presets,
    Settings
}

public sealed class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly DispatcherTimer _colorDebounce;
    private readonly DispatcherTimer _overlayDebounce;
    private readonly DispatcherTimer _zoomDebounce;
    private readonly DispatcherTimer _saveDebounce;

    private MonitorDevice? _selectedMonitor;
    private MonitorDevice? _selectedZoomMonitor;
    private DisplayModeOption? _selectedMode;
    private DisplayModeOption? _currentMode;
    private ColorPreset? _selectedPreset;
    private AppPage _page = AppPage.Display;
    private string _status = string.Empty;
    private string _newPresetName = string.Empty;
    private string _newPositionName = string.Empty;
    private SavedOverlayPosition? _selectedSavedPosition;
    private bool _capturingHotkey;
    private bool _capturingZoomHotkey;
    private bool _fineNudge = true;
    private bool _showAllDisplayModes;
    private bool _loggingEnabled;
    private bool _consoleEnabled;
    private bool _automaticStartup;
    private bool _minimizeToTrayOnClose;
    private bool _startMinimized;
    private bool _suppressApply;
    private bool _initialized;

    public MainViewModel(AppServices services)
    {
        _services = services;

        _colorDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _colorDebounce.Tick += (_, _) =>
        {
            _colorDebounce.Stop();
            ApplyColorNow();
        };

        _overlayDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _overlayDebounce.Tick += (_, _) =>
        {
            _overlayDebounce.Stop();
            ApplyOverlayNow();
            ApplyZoomNow();
        };

        _zoomDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _zoomDebounce.Tick += (_, _) =>
        {
            _zoomDebounce.Stop();
            ApplyZoomNow();
        };

        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _saveDebounce.Tick += (_, _) =>
        {
            _saveDebounce.Stop();
            PersistSettings();
        };

        GoDisplayCommand = new RelayCommand(() => Page = AppPage.Display);
        GoColorCommand = new RelayCommand(() => Page = AppPage.Color);
        GoOverlayCommand = new RelayCommand(() => Page = AppPage.Overlay);
        GoZoomCommand = new RelayCommand(() => Page = AppPage.Zoom);
        GoPresetsCommand = new RelayCommand(() => Page = AppPage.Presets);
        GoSettingsCommand = new RelayCommand(() => Page = AppPage.Settings);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin, () => !IsAdministrator);
        RefreshDisplaysCommand = new RelayCommand(RefreshDisplays);
        SelectMonitorCommand = new RelayCommand(param =>
        {
            if (param is MonitorDevice monitor)
            {
                SelectedMonitor = monitor;
            }
        });
        ApplyDisplayCommand = new RelayCommand(ApplyDisplay, () => CanApplyDisplay);
        ResetColorCommand = new RelayCommand(ResetColor);
        SetColourVisionCommand = new RelayCommand(SetColourVision);
        ApplyPresetCommand = new RelayCommand(ApplySelectedPreset, () => SelectedPreset is not null);
        SavePresetCommand = new RelayCommand(SavePreset, () => !string.IsNullOrWhiteSpace(NewPresetName));
        SaveLookCommand = new RelayCommand(SaveLook);
        DeletePresetCommand = new RelayCommand(DeleteSelectedPreset, () => SelectedPreset is not null);
        SetOverlayColorCommand = new RelayCommand(param =>
        {
            if (param is MediaColor color)
            {
                Overlay.Color = color;
            }
            else if (param is string hex && OverlayOptions.TryParseHex(hex, out var parsed))
            {
                Overlay.Color = parsed;
            }
        });
        DetectDisplaysCommand = new RelayCommand(() => _services.Identify.Show(Monitors));
        BrowseCrosshairCommand = new RelayCommand(BrowseCrosshair);
        CaptureHotkeyCommand = new RelayCommand(() =>
        {
            IsCapturingZoomHotkey = false;
            IsCapturingHotkey = true;
        });
        ClearHotkeyCommand = new RelayCommand(() =>
        {
            Overlay.HotkeyVk = 0;
            IsCapturingHotkey = false;
        });
        CaptureZoomHotkeyCommand = new RelayCommand(() =>
        {
            IsCapturingHotkey = false;
            IsCapturingZoomHotkey = true;
        });
        ClearZoomHotkeyCommand = new RelayCommand(() =>
        {
            Zoom.HotkeyVk = 0;
            IsCapturingZoomHotkey = false;
        });
        ExportPresetCommand = new RelayCommand(ExportPreset);
        ImportPresetCommand = new RelayCommand(ImportPreset);
        NudgeUpCommand = new RelayCommand(() => Overlay.Nudge(0, -NudgeStep));
        NudgeDownCommand = new RelayCommand(() => Overlay.Nudge(0, NudgeStep));
        NudgeLeftCommand = new RelayCommand(() => Overlay.Nudge(-NudgeStep, 0));
        NudgeRightCommand = new RelayCommand(() => Overlay.Nudge(NudgeStep, 0));
        OpenProjectsCommand = new RelayCommand(OpenProjects);
        OpenUpdatesCommand = new RelayCommand(OpenUpdates);
        SaveOverlayPositionCommand = new RelayCommand(SaveNamedPosition);
        LoadOverlayPositionCommand = new RelayCommand(LoadNamedPosition, () => SelectedSavedPosition is not null);
        DeleteSavedPositionCommand = new RelayCommand(DeleteNamedPosition, () => SelectedSavedPosition is not null);
        ResetOverlayCommand = new RelayCommand(ResetOverlayLook);

        Color.PropertyChanged += (_, _) =>
        {
            if (_initialized && !_suppressApply)
            {
                _colorDebounce.Stop();
                _colorDebounce.Start();
                _saveDebounce.Stop();
                _saveDebounce.Start();
            }
        };

        Overlay.PropertyChanged += (_, _) =>
        {
            if (_initialized && !_suppressApply)
            {
                _overlayDebounce.Stop();
                _overlayDebounce.Start();
                _saveDebounce.Stop();
                _saveDebounce.Start();
            }
        };

        Zoom.PropertyChanged += (_, _) =>
        {
            if (_initialized && !_suppressApply)
            {
                _zoomDebounce.Stop();
                _zoomDebounce.Start();
                _saveDebounce.Stop();
                _saveDebounce.Start();
            }
        };
    }

    public Func<string, bool>? ConfirmKeepDisplayMode { get; set; }
    public Func<string, string, string?>? PromptForName { get; set; }

    public ObservableCollection<MonitorDevice> Monitors { get; } = new();
    public ObservableCollection<DisplayModeOption> Modes { get; } = new();
    public ObservableCollection<ColorPreset> Presets { get; } = new();
    public ObservableCollection<string> LogLines => _services.Log.Lines;

    public ColorAdjustment Color { get; } = new();
    public OverlayOptions Overlay { get; } = new();
    public ZoomOptions Zoom { get; } = new();

    public IReadOnlyList<CrosshairStyle> OverlayStyles { get; } = Enum.GetValues<CrosshairStyle>();
    public IReadOnlyList<OverlayHotkeyMode> HotkeyModes { get; } = Enum.GetValues<OverlayHotkeyMode>();

    public IReadOnlyList<MediaColor> OverlaySwatches { get; } =
    [
        MediaColor.FromRgb(0x7E, 0xC8, 0xE3),
        MediaColor.FromRgb(0xF5, 0xF7, 0xFA),
        MediaColor.FromRgb(0xFF, 0x4D, 0x4D),
        MediaColor.FromRgb(0x3D, 0xFF, 0x7A),
        MediaColor.FromRgb(0xFF, 0xE5, 0x66),
        MediaColor.FromRgb(0xFF, 0x4D, 0xDB),
        MediaColor.FromRgb(0x4D, 0xFF, 0xF3)
    ];

    public ICommand GoDisplayCommand { get; }
    public ICommand GoColorCommand { get; }
    public ICommand GoOverlayCommand { get; }
    public ICommand GoZoomCommand { get; }
    public ICommand GoPresetsCommand { get; }
    public ICommand GoSettingsCommand { get; }
    public ICommand RestartAsAdminCommand { get; }
    public ICommand RefreshDisplaysCommand { get; }
    public ICommand SelectMonitorCommand { get; }
    public ICommand ApplyDisplayCommand { get; }
    public ICommand ResetColorCommand { get; }
    public ICommand SetColourVisionCommand { get; }
    public ICommand ApplyPresetCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand SaveLookCommand { get; }
    public ICommand DeletePresetCommand { get; }
    public ICommand SetOverlayColorCommand { get; }
    public ICommand DetectDisplaysCommand { get; }
    public ICommand BrowseCrosshairCommand { get; }
    public ICommand CaptureHotkeyCommand { get; }
    public ICommand ClearHotkeyCommand { get; }
    public ICommand CaptureZoomHotkeyCommand { get; }
    public ICommand ClearZoomHotkeyCommand { get; }
    public ICommand ExportPresetCommand { get; }
    public ICommand ImportPresetCommand { get; }
    public ICommand NudgeUpCommand { get; }
    public ICommand NudgeDownCommand { get; }
    public ICommand NudgeLeftCommand { get; }
    public ICommand NudgeRightCommand { get; }
    public ICommand SaveOverlayPositionCommand { get; }
    public ICommand LoadOverlayPositionCommand { get; }
    public ICommand DeleteSavedPositionCommand { get; }
    public ICommand ResetOverlayCommand { get; }
    public ICommand OpenProjectsCommand { get; }
    public ICommand OpenUpdatesCommand { get; }
    public string VersionLabel { get; } = "V1.0";

    public AppPage Page
    {
        get => _page;
        set
        {
            if (SetField(ref _page, value))
            {
                OnPropertyChanged(nameof(IsDisplayPage));
                OnPropertyChanged(nameof(IsColorPage));
                OnPropertyChanged(nameof(IsOverlayPage));
                OnPropertyChanged(nameof(IsZoomPage));
                OnPropertyChanged(nameof(IsPresetsPage));
                OnPropertyChanged(nameof(IsSettingsPage));
            }
        }
    }

    public bool IsDisplayPage => Page == AppPage.Display;
    public bool IsColorPage => Page == AppPage.Color;
    public bool IsOverlayPage => Page == AppPage.Overlay;
    public bool IsZoomPage => Page == AppPage.Zoom;
    public bool IsPresetsPage => Page == AppPage.Presets;
    public bool IsSettingsPage => Page == AppPage.Settings;

    public bool IsAdministrator => Elevation.IsAdministrator;

    public string ElevationLabel => IsAdministrator ? "Running as administrator" : "Not running as administrator";

    public MonitorDevice? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            if (SetField(ref _selectedMonitor, value))
            {
                foreach (var monitor in Monitors)
                {
                    monitor.IsSelected = ReferenceEquals(monitor, value);
                }

                ReloadModes();
                if (_initialized)
                {
                    ApplyColorNow();
                    ApplyOverlayNow();
                    ApplyZoomNow();
                    ScheduleSave();
                }
            }
        }
    }

    public MonitorDevice? SelectedZoomMonitor
    {
        get => _selectedZoomMonitor;
        set
        {
            if (SetField(ref _selectedZoomMonitor, value))
            {
                Zoom.TargetDeviceName = value?.DeviceName;
                if (_initialized && !_suppressApply)
                {
                    ApplyZoomNow();
                    ScheduleSave();
                }
            }
        }
    }

    public DisplayModeOption? SelectedMode
    {
        get => _selectedMode;
        set => SetField(ref _selectedMode, value);
    }

    public DisplayModeOption? CurrentMode
    {
        get => _currentMode;
        private set
        {
            if (SetField(ref _currentMode, value))
            {
                OnPropertyChanged(nameof(CurrentModeLabel));
                OnPropertyChanged(nameof(CanApplyDisplay));
            }
        }
    }

    public string CurrentModeLabel => CurrentMode is null
        ? "Current mode unavailable"
        : CurrentMode.ShortLabel;

    public bool ShowAllDisplayModes
    {
        get => _showAllDisplayModes;
        set
        {
            if (SetField(ref _showAllDisplayModes, value))
            {
                ReloadModes();
                ScheduleSave();
            }
        }
    }

    public bool CanApplyDisplay =>
        SelectedMonitor is not null
        && SelectedMode is not null
        && CurrentMode is not null
        && !SelectedMode.Equals(CurrentMode);

    public ColorPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetField(ref _selectedPreset, value) && _initialized && !_suppressApply && value is not null)
            {
                ApplySelectedPreset();
            }
        }
    }

    public string NewPresetName
    {
        get => _newPresetName;
        set => SetField(ref _newPresetName, value);
    }

    public string NewPositionName
    {
        get => _newPositionName;
        set => SetField(ref _newPositionName, value);
    }

    public SavedOverlayPosition? SelectedSavedPosition
    {
        get => _selectedSavedPosition;
        set
        {
            if (SetField(ref _selectedSavedPosition, value) && _initialized && !_suppressApply && value is not null)
            {
                LoadNamedPosition();
            }
        }
    }

    public bool IsCapturingHotkey
    {
        get => _capturingHotkey;
        set
        {
            if (SetField(ref _capturingHotkey, value))
            {
                OnPropertyChanged(nameof(CaptureHotkeyLabel));
            }
        }
    }

    public string CaptureHotkeyLabel => IsCapturingHotkey ? "Press a key..." : "Set key";

    public bool IsCapturingZoomHotkey
    {
        get => _capturingZoomHotkey;
        set
        {
            if (SetField(ref _capturingZoomHotkey, value))
            {
                OnPropertyChanged(nameof(CaptureZoomHotkeyLabel));
            }
        }
    }

    public string CaptureZoomHotkeyLabel => IsCapturingZoomHotkey ? "Press a key..." : "Set key";

    public bool FineNudge
    {
        get => _fineNudge;
        set => SetField(ref _fineNudge, value);
    }

    private int NudgeStep => FineNudge ? 1 : 5;

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public bool LoggingEnabled
    {
        get => _loggingEnabled;
        set
        {
            if (SetField(ref _loggingEnabled, value))
            {
                _services.Log.LoggingEnabled = value;
                ScheduleSave();
            }
        }
    }

    public bool ConsoleEnabled
    {
        get => _consoleEnabled;
        set
        {
            if (SetField(ref _consoleEnabled, value))
            {
                _services.Log.ConsoleEnabled = value;
                DebugConsole.SetVisible(value);
                ScheduleSave();
            }
        }
    }

    public bool AutomaticStartup
    {
        get => _automaticStartup;
        set
        {
            if (SetField(ref _automaticStartup, value))
            {
                if (_initialized)
                {
                    StartupRegistration.SetEnabled(value);
                    ScheduleSave();
                }
            }
        }
    }

    public bool MinimizeToTrayOnClose
    {
        get => _minimizeToTrayOnClose;
        set
        {
            if (SetField(ref _minimizeToTrayOnClose, value))
            {
                ScheduleSave();
            }
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set
        {
            if (SetField(ref _startMinimized, value))
            {
                ScheduleSave();
            }
        }
    }

    public void Initialize()
    {
        _suppressApply = true;
        ShowAllDisplayModes = _services.CurrentSettings.ShowAllDisplayModes;
        LoggingEnabled = _services.CurrentSettings.LoggingEnabled;
        ConsoleEnabled = _services.CurrentSettings.ConsoleEnabled;
        AutomaticStartup = _services.CurrentSettings.AutomaticStartup;
        MinimizeToTrayOnClose = _services.CurrentSettings.MinimizeToTrayOnClose;
        StartMinimized = _services.CurrentSettings.StartMinimized;
        Color.CopyFrom(_services.CurrentSettings.Color);
        Overlay.CopyFrom(_services.CurrentSettings.Overlay);
        Zoom.CopyFrom(_services.CurrentSettings.Zoom);
        SelectedSavedPosition = Overlay.SavedPositions.FirstOrDefault();
        LoadPresets();
        RefreshDisplays();

        string? savedDevice = _services.CurrentSettings.SelectedMonitorDeviceName;
        SelectedMonitor = Monitors.FirstOrDefault(m =>
                               string.Equals(m.DeviceName, savedDevice, StringComparison.OrdinalIgnoreCase))
                           ?? Monitors.FirstOrDefault(m => m.IsPrimary)
                           ?? Monitors.FirstOrDefault();

        SelectedZoomMonitor = Monitors.FirstOrDefault(m =>
                                  string.Equals(m.DeviceName, Zoom.TargetDeviceName, StringComparison.OrdinalIgnoreCase))
                              ?? SelectedMonitor;

        _suppressApply = false;
        _initialized = true;
        StartupRegistration.SetEnabled(AutomaticStartup);
        ApplyColorNow(log: !Color.IsDefault);
        ApplyOverlayNow();
        ApplyZoomNow();
    }

    public void PersistSettings()
    {
        _services.Save(new AppSettings
        {
            SelectedMonitorDeviceName = SelectedMonitor?.DeviceName,
            ShowAllDisplayModes = ShowAllDisplayModes,
            LoggingEnabled = LoggingEnabled,
            ConsoleEnabled = ConsoleEnabled,
            AutomaticStartup = AutomaticStartup,
            MinimizeToTrayOnClose = MinimizeToTrayOnClose,
            StartMinimized = StartMinimized,
            Color = Color.Clone(),
            Overlay = CloneOverlay(Overlay),
            Zoom = Zoom.Clone()
        });
    }

    private static OverlayOptions CloneOverlay(OverlayOptions source)
    {
        var copy = new OverlayOptions();
        copy.CopyFrom(source);
        return copy;
    }

    private void RefreshDisplays()
    {
        string? selectedName = SelectedMonitor?.DeviceName;
        Monitors.Clear();
        foreach (var monitor in _services.Display.GetMonitors())
        {
            Monitors.Add(monitor);
        }

        if (Monitors.Count == 0)
        {
            SelectedMonitor = null;
            SelectedZoomMonitor = null;
            Status = "No active displays were reported by Windows.";
            return;
        }

        var match = Monitors.FirstOrDefault(m =>
                        string.Equals(m.DeviceName, selectedName, StringComparison.OrdinalIgnoreCase))
                    ?? Monitors.FirstOrDefault(m => m.IsPrimary)
                    ?? Monitors[0];

        if (!ReferenceEquals(SelectedMonitor, match))
        {
            SelectedMonitor = match;
        }
        else
        {
            ReloadModes();
        }

        string? zoomName = Zoom.TargetDeviceName;
        SelectedZoomMonitor = Monitors.FirstOrDefault(m =>
                                  string.Equals(m.DeviceName, zoomName, StringComparison.OrdinalIgnoreCase))
                              ?? SelectedMonitor;
    }

    private void ReloadModes()
    {
        Modes.Clear();
        CurrentMode = null;
        SelectedMode = null;

        if (SelectedMonitor is null)
        {
            return;
        }

        foreach (var mode in _services.Display.GetModes(SelectedMonitor.DeviceName, ShowAllDisplayModes))
        {
            Modes.Add(mode);
        }

        CurrentMode = _services.Display.GetCurrentMode(SelectedMonitor.DeviceName);
        SelectedMode = CurrentMode is null
            ? Modes.FirstOrDefault()
            : Modes.FirstOrDefault(m => m.Equals(CurrentMode)) ?? Modes.FirstOrDefault();
    }

    private void ApplyDisplay()
    {
        if (SelectedMonitor is null || SelectedMode is null)
        {
            return;
        }

        var previous = CurrentMode ?? _services.Display.GetCurrentMode(SelectedMonitor.DeviceName);
        if (previous is null)
        {
            Status = "Could not read the current desktop mode.";
            return;
        }

        var result = _services.Display.ApplyMode(SelectedMonitor.DeviceName, SelectedMode);
        if (!result.Success)
        {
            Status = result.Message;
            return;
        }

        _services.Log.Info(
            $"Display mode applied: {SelectedMode.ShortLabel} on {SelectedMonitor.DeviceName}");

        bool keep = ConfirmKeepDisplayMode?.Invoke(SelectedMode.ShortLabel) ?? true;
        if (!keep)
        {
            var revert = _services.Display.ApplyMode(SelectedMonitor.DeviceName, previous);
            Status = revert.Success
                ? "Display mode reverted."
                : "Could not revert the previous desktop mode.";
            if (revert.Success)
            {
                _services.Log.Info(
                    $"Display mode reverted: {previous.ShortLabel} on {SelectedMonitor.DeviceName}");
            }

            RefreshDisplays();
            return;
        }

        Status = result.RestartRequired ? "Restart required to finish." : "Display mode applied.";
        RefreshDisplays();
        ApplyOverlayNow();
        ScheduleSave();
    }

    private void ApplyColorNow(bool log = false)
    {
        if (SelectedMonitor is null)
        {
            return;
        }

        _services.Color.Apply(SelectedMonitor, Color, log);
    }

    private void ApplyOverlayNow() => _services.Overlay.Apply(Overlay, SelectedMonitor);

    private void ApplyZoomNow()
    {
        _services.Zoom.Apply(
            Zoom,
            SelectedZoomMonitor ?? SelectedMonitor,
            Overlay.OffsetX,
            Overlay.OffsetY,
            () => [_services.Overlay.WindowHandle],
            _services.Overlay.BringToFront);
    }

    private void SetColourVision(object? parameter)
    {
        if (parameter is ColourVisionMode mode)
        {
            Color.ColourVision = mode;
            return;
        }

        if (parameter is string text && Enum.TryParse(text, out ColourVisionMode parsed))
        {
            Color.ColourVision = parsed;
        }
    }

    private void ResetColor()
    {
        _suppressApply = true;
        Color.ResetToNeutral();
        _suppressApply = false;
        _services.Color.Restore(SelectedMonitor);
        ApplyColorNow();
        Status = "Colour reset.";
        ScheduleSave();
    }

    private void LoadPresets()
    {
        Presets.Clear();
        foreach (var preset in _services.Presets.LoadAll())
        {
            Presets.Add(preset);
        }

        SelectedPreset = Presets.FirstOrDefault();
    }

    private void ApplySelectedPreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        _suppressApply = true;
        SelectedPreset.ApplyTo(Color);
        _suppressApply = false;
        ApplyColorNow(log: true);
        _services.Log.Info($"Preset applied: {SelectedPreset.Name}");
        Status = $"Applied preset “{SelectedPreset.Name}”.";
        ScheduleSave();
    }

    private void SaveLook()
    {
        string suggested = !string.IsNullOrWhiteSpace(NewPresetName)
            ? NewPresetName.Trim()
            : SelectedPreset?.Name ?? string.Empty;
        string? name = PromptForName?.Invoke("Save look", suggested);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        NewPresetName = name.Trim();
        SavePreset();
    }

    private void SavePreset()
    {
        string name = NewPresetName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        var existing = Presets.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Brightness = Color.Brightness;
            existing.Contrast = Color.Contrast;
            existing.Gamma = Color.Gamma;
            existing.Saturation = Color.Saturation;
            SelectedPreset = existing;
        }
        else
        {
            var created = ColorPreset.FromAdjustment(name, Color);
            Presets.Add(created);
            SelectedPreset = created;
        }

        PersistUserPresets();
        _services.Log.Info($"Preset saved: {name}");
        NewPresetName = string.Empty;
        Status = $"Saved preset “{name}”.";
    }

    private void DeleteSelectedPreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        string name = SelectedPreset.Name;
        Presets.Remove(SelectedPreset);
        PersistUserPresets();
        _services.Log.Info($"Preset deleted: {name}");
        _suppressApply = true;
        SelectedPreset = Presets.FirstOrDefault();
        _suppressApply = false;
        Status = $"Deleted preset “{name}”.";
    }

    private void PersistUserPresets()
    {
        try
        {
            _services.Presets.SaveUserPresets(Presets);
        }
        catch (Exception ex)
        {
            Status = "Could not save presets: " + ex.Message;
        }
    }

    private void RestartAsAdmin()
    {
        PersistSettings();
        if (Elevation.TryRestartElevated())
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void SaveNamedPosition()
    {
        string suggested = !string.IsNullOrWhiteSpace(NewPositionName)
            ? NewPositionName.Trim()
            : SelectedSavedPosition?.Name ?? string.Empty;
        string? name = PromptForName?.Invoke("Save position", suggested)?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var existing = Overlay.SavedPositions.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        var created = new SavedOverlayPosition
        {
            Name = existing?.Name ?? name,
            X = Overlay.OffsetX,
            Y = Overlay.OffsetY
        };

        if (existing is not null)
        {
            int index = Overlay.SavedPositions.IndexOf(existing);
            Overlay.SavedPositions[index] = created;
        }
        else
        {
            Overlay.SavedPositions.Add(created);
        }

        _suppressApply = true;
        SelectedSavedPosition = created;
        _suppressApply = false;

        NewPositionName = string.Empty;
        ScheduleSave();
    }

    private void LoadNamedPosition()
    {
        if (SelectedSavedPosition is null)
        {
            return;
        }

        Overlay.OffsetX = SelectedSavedPosition.X;
        Overlay.OffsetY = SelectedSavedPosition.Y;
    }

    private void DeleteNamedPosition()
    {
        if (SelectedSavedPosition is null)
        {
            return;
        }

        Overlay.SavedPositions.Remove(SelectedSavedPosition);
        _suppressApply = true;
        SelectedSavedPosition = Overlay.SavedPositions.FirstOrDefault();
        _suppressApply = false;
        ScheduleSave();
    }

    private void ResetOverlayLook()
    {
        Overlay.ResetLook();
        Status = "Crosshair customisation reset.";
        ScheduleSave();
    }

    public void SetHotkey(int virtualKey)
    {
        Overlay.HotkeyVk = virtualKey;
        IsCapturingHotkey = false;
    }

    public void SetZoomHotkey(int virtualKey)
    {
        Zoom.HotkeyVk = virtualKey;
        IsCapturingZoomHotkey = false;
    }

    private static void OpenProjects()
    {
        OpenUrl("https://github.com/soh4p");
    }

    private static void OpenUpdates()
    {
        OpenUrl("https://github.com/soh4p/ICY");
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void BrowseCrosshair()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IcyPaths.EnsureRoot();
            string dest = Path.Combine(IcyPaths.Root, "crosshair" + Path.GetExtension(dialog.FileName));
            File.Copy(dialog.FileName, dest, true);
            Overlay.CustomImagePath = dest;
            Overlay.Style = CrosshairStyle.Custom;
        }
        catch
        {
            Overlay.CustomImagePath = dialog.FileName;
            Overlay.Style = CrosshairStyle.Custom;
        }
    }

    private void ExportPreset()
    {
        string name = NewPresetName.Trim();
        ColorPreset preset;
        if (name.Length > 0)
        {
            preset = ColorPreset.FromAdjustment(name, Color);
        }
        else if (SelectedPreset is not null)
        {
            preset = SelectedPreset;
        }
        else
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ICY preset|*.json",
            FileName = SanitizeFileName(preset.Name) + ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            _services.Presets.Export(preset, dialog.FileName);
        }
    }

    private void ImportPreset()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ICY preset|*.json|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            foreach (var preset in _services.Presets.Import(dialog.FileName))
            {
                var existing = Presets.FirstOrDefault(p =>
                    string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                {
                    existing.Brightness = preset.Brightness;
                    existing.Contrast = preset.Contrast;
                    existing.Gamma = preset.Gamma;
                    existing.Saturation = preset.Saturation;
                    SelectedPreset = existing;
                }
                else
                {
                    Presets.Add(preset);
                    SelectedPreset = preset;
                }
            }

            PersistUserPresets();
        }
        catch
        {
            // Ignore unreadable files.
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "preset" : name;
    }

    private void ScheduleSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }
}
