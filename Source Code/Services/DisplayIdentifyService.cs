using System.Windows.Threading;
using ICY.Models;
using ICY.Views;

namespace ICY.Services;

public sealed class DisplayIdentifyService
{
    private readonly List<IdentifyWindow> _windows = [];
    private DispatcherTimer? _timer;

    public void Show(IEnumerable<MonitorDevice> monitors)
    {
        Hide();
        foreach (var monitor in monitors)
        {
            var window = new IdentifyWindow(monitor.Number);
            window.Show();
            window.Place(monitor);
            _windows.Add(window);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (_, _) => Hide();
        _timer.Start();
    }

    public void Hide()
    {
        _timer?.Stop();
        _timer = null;
        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();
    }
}
