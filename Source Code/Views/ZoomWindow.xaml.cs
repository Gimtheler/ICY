using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ICY.Models;
using ICY.Native;

namespace ICY.Views;

public partial class ZoomWindow : Window
{
    public ZoomWindow()
    {
        InitializeComponent();
    }

    public IntPtr HostHandle
    {
        get
        {
            var helper = new WindowInteropHelper(this);
            return helper.Handle;
        }
    }

    public IntPtr MagnifierHandle => Lens.MagnifierHandle;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplySafeWindowStyles();
    }

    public void ApplyVisuals(ZoomOptions options, MonitorDevice? monitor, double offsetX, double offsetY, IEnumerable<IntPtr> exclude)
    {
        int outline = options.OutlinePixels;
        int diameter = PhysicalSize(options.Size, monitor);
        int outer = diameter + (outline * 2);

        Background = options.Outline switch
        {
            ZoomOutline.White => Brushes.White,
            ZoomOutline.Black => Brushes.Black,
            _ => Brushes.Black
        };

        Width = outer;
        Height = outer;
        Lens.Margin = new Thickness(outline);
        Lens.Width = diameter;
        Lens.Height = diameter;

        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.EnsureHandle();
        ApplySafeWindowStyles();
        ApplyOuterCircle(hwnd, outer);
        Lens.ResizeLens(diameter, diameter);
        Lens.SetZoom(options.Factor);
        Lens.Exclude(exclude.Append(hwnd).Append(Lens.MagnifierHandle).ToArray());
        PlaceOnMonitor(hwnd, outer, monitor, offsetX, offsetY);
        RefreshSource(options, monitor, offsetX, offsetY);
    }

    public void RefreshSource(ZoomOptions options, MonitorDevice? monitor, double offsetX, double offsetY)
    {
        int diameter = PhysicalSize(options.Size, monitor);
        int source = Math.Max(8, (int)Math.Round(diameter / options.Factor));
        CenterPoint(monitor, offsetX, offsetY, out int cx, out int cy);
        Lens.SetSource(
            cx - source / 2,
            cy - source / 2,
            cx + (source + 1) / 2,
            cy + (source + 1) / 2);
    }

    private void ApplySafeWindowStyles()
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.EnsureHandle();
        int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
        int next = style
                   | NativeMethods.WsExTransparent
                   | NativeMethods.WsExToolWindow
                   | NativeMethods.WsExNoActivate
                   | NativeMethods.WsExTopmost;
        if (next != style)
        {
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GwlExStyle, next);
        }
    }

    private static void ApplyOuterCircle(IntPtr hwnd, int outer)
    {
        IntPtr region = NativeMethods.CreateEllipticRgn(0, 0, outer, outer);
        if (region != IntPtr.Zero)
        {
            NativeMethods.SetWindowRgn(hwnd, region, true);
        }
    }

    private void PlaceOnMonitor(IntPtr hwnd, int outer, MonitorDevice? monitor, double offsetX, double offsetY)
    {
        CenterPoint(monitor, offsetX, offsetY, out int cx, out int cy);
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            cx - outer / 2,
            cy - outer / 2,
            outer,
            outer,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoCopyBits);
    }

    private static int PhysicalSize(double size, MonitorDevice? monitor)
    {
        double dpi = monitor?.DpiX ?? 96;
        return Math.Max(32, (int)Math.Round(size * dpi / 96.0));
    }

    private static void CenterPoint(MonitorDevice? monitor, double offsetX, double offsetY, out int x, out int y)
    {
        if (monitor is null)
        {
            x = (int)Math.Round(SystemParameters.PrimaryScreenWidth / 2.0 + offsetX);
            y = (int)Math.Round(SystemParameters.PrimaryScreenHeight / 2.0 + offsetY);
            return;
        }

        x = monitor.PixelLeft + monitor.PixelWidth / 2 + (int)Math.Round(offsetX);
        y = monitor.PixelTop + monitor.PixelHeight / 2 + (int)Math.Round(offsetY);
    }
}
