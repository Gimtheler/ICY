using System.ComponentModel;
using System.Windows;
using ICY.Services;
using ICY.ViewModels;

namespace ICY.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly TrayIconService _tray = new();
    private bool _forceClose;
    private bool _launchApplied;

    public MainWindow(AppServices services)
    {
        InitializeComponent();
        _viewModel = new MainViewModel(services)
        {
            ConfirmKeepDisplayMode = summary =>
            {
                var dialog = new ConfirmDisplayChangeWindow(summary) { Owner = this };
                return dialog.ShowDialog() == true;
            },
            PromptForName = (title, suggested) =>
            {
                var dialog = new NamePromptWindow(title, suggested) { Owner = this };
                return dialog.ShowDialog() == true ? dialog.ResultName : null;
            }
        };
        DataContext = _viewModel;
        _tray.OpenRequested += RestoreFromTray;
        _tray.ExitRequested += ExitFromTray;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Loaded += OnLoaded;
    }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Opacity = 1;
        Activate();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_forceClose && _viewModel.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        _viewModel.PersistSettings();
        _tray.Dispose();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        base.OnClosed(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_launchApplied)
        {
            return;
        }

        _launchApplied = true;
        _viewModel.Initialize();
        ApplyLaunchState();
    }

    private void ApplyLaunchState()
    {
        UpdateTrayVisibility();
        if (_viewModel.StartMinimized && _viewModel.MinimizeToTrayOnClose)
        {
            HideToTray();
        }
        else if (_viewModel.StartMinimized)
        {
            WindowState = WindowState.Minimized;
        }

        Opacity = 1;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MinimizeToTrayOnClose))
        {
            UpdateTrayVisibility();
        }
    }

    private void UpdateTrayVisibility()
        => _tray.Visible = _viewModel.MinimizeToTrayOnClose;

    private void HideToTray()
    {
        _tray.Visible = true;
        ShowInTaskbar = false;
        Hide();
    }

    private void ExitFromTray()
    {
        _forceClose = true;
        Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
