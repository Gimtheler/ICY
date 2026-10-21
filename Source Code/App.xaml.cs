using System.Windows;
using ICY.Services;
using ICY.Views;

namespace ICY;

public partial class App : Application
{
    private AppServices? _services;
    private InstanceGuard? _instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instance = new InstanceGuard(() =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (MainWindow is MainWindow window)
                {
                    window.RestoreFromTray();
                }
            });
        });

        if (!_instance.IsPrimary)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        _services = new AppServices();
        _services.Start();

        var window = new MainWindow(_services);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }
}
