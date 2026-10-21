using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ICY.Models;
using ICY.Native;

namespace ICY.Views;

public partial class OverlayWindow : Window
{
    private const double CanvasSize = 256;

    public OverlayWindow()
    {
        InitializeComponent();
        Width = CanvasSize;
        Height = CanvasSize;
        Stage.Width = CanvasSize;
        Stage.Height = CanvasSize;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplySafeWindowStyles();
    }

    public void ApplyVisuals(OverlayOptions options, MonitorDevice? monitor)
    {
        Opacity = options.Opacity;
        RebuildGraphic(options);
        PlaceOnMonitor(options, monitor);
        ApplySafeWindowStyles();
        BringToFront();
    }

    public void BringToFront()
    {
        if (!IsVisible)
        {
            return;
        }

        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove
            | NativeMethods.SwpNoSize
            | NativeMethods.SwpNoActivate
            | NativeMethods.SwpNoOwnerZOrder
            | NativeMethods.SwpNoCopyBits);
    }

    private void ApplySafeWindowStyles()
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.EnsureHandle();
        int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GwlExStyle);
        int next = style
                   | NativeMethods.WsExTransparent
                   | NativeMethods.WsExLayered
                   | NativeMethods.WsExToolWindow
                   | NativeMethods.WsExNoActivate
                   | NativeMethods.WsExTopmost;
        if (next != style)
        {
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GwlExStyle, next);
        }
    }

    private void PlaceOnMonitor(OverlayOptions options, MonitorDevice? monitor)
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.EnsureHandle();
        int box = monitor is null
            ? (int)CanvasSize
            : (int)Math.Round(CanvasSize * monitor.DpiX / 96.0);

        int left;
        int top;
        if (monitor is null)
        {
            left = (int)Math.Round((SystemParameters.PrimaryScreenWidth - CanvasSize) / 2.0 + options.OffsetX);
            top = (int)Math.Round((SystemParameters.PrimaryScreenHeight - CanvasSize) / 2.0 + options.OffsetY);
        }
        else
        {
            left = monitor.PixelLeft
                   + (int)Math.Round((monitor.PixelWidth - box) / 2.0 + options.OffsetX);
            top = monitor.PixelTop
                  + (int)Math.Round((monitor.PixelHeight - box) / 2.0 + options.OffsetY);
        }

        Width = CanvasSize;
        Height = CanvasSize;
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTopmost,
            left,
            top,
            box,
            box,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoCopyBits);
    }

    private void RebuildGraphic(OverlayOptions options)
    {
        Stage.Children.Clear();
        if (options.Style == CrosshairStyle.Custom && TryAddCustomImage(options))
        {
            return;
        }

        var brush = new SolidColorBrush(options.Color);
        brush.Freeze();

        double cx = CanvasSize / 2.0;
        double cy = CanvasSize / 2.0;
        double arm = options.Size;
        double gap = options.Gap;
        double t = options.Thickness;

        bool cross = options.Style is CrosshairStyle.Cross or CrosshairStyle.CrossAndDot;
        bool tShape = options.Style is CrosshairStyle.TShape;
        bool xMark = options.Style is CrosshairStyle.X;
        bool circle = options.Style is CrosshairStyle.Circle;
        bool dot = options.Style is CrosshairStyle.Dot or CrosshairStyle.CrossAndDot || options.DotCenter;

        if (cross || tShape)
        {
            AddLine(cx - arm, cy, cx - gap, cy, brush, t);
            AddLine(cx + gap, cy, cx + arm, cy, brush, t);
            if (cross)
            {
                AddLine(cx, cy - arm, cx, cy - gap, brush, t);
            }

            AddLine(cx, cy + gap, cx, cy + arm, brush, t);
        }

        if (xMark)
        {
            double inner = gap / Math.Sqrt(2.0);
            if (inner < 0.5)
            {
                AddLine(cx - arm, cy - arm, cx + arm, cy + arm, brush, t);
                AddLine(cx - arm, cy + arm, cx + arm, cy - arm, brush, t);
            }
            else if (inner < arm)
            {
                AddLine(cx - arm, cy - arm, cx - inner, cy - inner, brush, t);
                AddLine(cx + inner, cy + inner, cx + arm, cy + arm, brush, t);
                AddLine(cx + arm, cy - arm, cx + inner, cy - inner, brush, t);
                AddLine(cx - inner, cy + inner, cx - arm, cy + arm, brush, t);
            }
        }

        if (circle)
        {
            var ring = new Ellipse
            {
                Width = arm * 2,
                Height = arm * 2,
                Stroke = brush,
                StrokeThickness = t,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ring, cx - arm);
            Canvas.SetTop(ring, cy - arm);
            Stage.Children.Add(ring);
        }

        if (dot)
        {
            double d = Math.Max(1, options.DotSize);
            var mark = new Ellipse
            {
                Width = d,
                Height = d,
                Fill = brush,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(mark, cx - d / 2);
            Canvas.SetTop(mark, cy - d / 2);
            Stage.Children.Add(mark);
        }
    }

    private bool TryAddCustomImage(OverlayOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CustomImagePath) || !File.Exists(options.CustomImagePath))
        {
            return false;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(options.CustomImagePath);
            image.EndInit();
            image.Freeze();

            double size = Math.Clamp(options.Size * 3.2, 16, CanvasSize - 8);
            var view = new Image
            {
                Source = image,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(view, (CanvasSize - size) / 2);
            Canvas.SetTop(view, (CanvasSize - size) / 2);
            Stage.Children.Add(view);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AddLine(double x1, double y1, double x2, double y2, Brush brush, double thickness)
    {
        Stage.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = brush,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        });
    }
}
