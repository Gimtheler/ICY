using System.Windows;
using System.Windows.Interop;
using ICY.Models;
using ICY.Native;

namespace ICY.Views;

public partial class IdentifyWindow : Window
{
    public IdentifyWindow(int number)
    {
        InitializeComponent();
        NumberText.Text = number.ToString();
    }

    public void Place(MonitorDevice monitor)
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.EnsureHandle();
        int box = (int)Math.Round(160.0 * monitor.DpiX / 96.0);
        int x = monitor.PixelLeft + (monitor.PixelWidth - box) / 2;
        int y = monitor.PixelTop + (monitor.PixelHeight - box) / 2;
        Width = 160;
        Height = 160;
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            x,
            y,
            box,
            box,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
    }
}
