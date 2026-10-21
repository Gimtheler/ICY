using System.Windows;
using System.Windows.Threading;

namespace ICY.Views;

public partial class ConfirmDisplayChangeWindow : Window
{
    private readonly DispatcherTimer _timer;
    private int _seconds = 15;

    public ConfirmDisplayChangeWindow(string modeSummary)
    {
        InitializeComponent();
        SummaryText.Text = modeSummary;
        UpdateCountdown();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            _seconds--;
            if (_seconds <= 0)
            {
                _timer.Stop();
                DialogResult = false;
                return;
            }

            UpdateCountdown();
        };
        _timer.Start();
    }

    private void UpdateCountdown()
        => CountdownText.Text = $"Reverting in {_seconds}s";

    private void Keep_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = true;
    }

    private void Revert_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = false;
    }
}
