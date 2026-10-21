using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICY.Infrastructure;

namespace ICY.Views;

public partial class ColorPicker : UserControl
{
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor),
        typeof(Color),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(
            Color.FromRgb(0x7E, 0xC8, 0xE3),
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnColorChanged));

    private bool _draggingSv;
    private bool _draggingHue;
    private bool _updating;
    private double _h = 190;
    private double _s = 0.45;
    private double _v = 0.89;

    public ColorPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncFromColor(SelectedColor);
    }

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPicker picker && !picker._updating)
        {
            picker.SyncFromColor((Color)e.NewValue);
        }
    }

    private void SyncFromColor(Color color)
    {
        Hsv.FromColor(color, out _h, out _s, out _v);
        UpdateVisuals();
    }

    private void Sv_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = true;
        SvHost.CaptureMouse();
        UpdateSv(e.GetPosition(SvHost));
    }

    private void Sv_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingSv)
        {
            UpdateSv(e.GetPosition(SvHost));
        }
    }

    private void Sv_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingSv = false;
        SvHost.ReleaseMouseCapture();
    }

    private void Hue_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = true;
        ((IInputElement)sender).CaptureMouse();
        UpdateHue(e.GetPosition((IInputElement)sender).Y, ((FrameworkElement)sender).ActualHeight);
    }

    private void Hue_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingHue)
        {
            UpdateHue(e.GetPosition((IInputElement)sender).Y, ((FrameworkElement)sender).ActualHeight);
        }
    }

    private void Hue_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _draggingHue = false;
        ((IInputElement)sender).ReleaseMouseCapture();
    }

    private void UpdateSv(Point point)
    {
        double w = Math.Max(1, SvHost.ActualWidth);
        double h = Math.Max(1, SvHost.ActualHeight);
        _s = Math.Clamp(point.X / w, 0, 1);
        _v = Math.Clamp(1.0 - (point.Y / h), 0, 1);
        PushColor();
    }

    private void UpdateHue(double y, double height)
    {
        _h = Math.Clamp(y / Math.Max(1, height), 0, 1) * 360.0;
        PushColor();
    }

    private void PushColor()
    {
        _updating = true;
        SelectedColor = Hsv.ToColor(_h, _s, _v);
        _updating = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        HueBase.Fill = new SolidColorBrush(Hsv.ToColor(_h, 1, 1));
        Preview.Background = new SolidColorBrush(Hsv.ToColor(_h, _s, _v));
        double w = Math.Max(1, SvHost.ActualWidth);
        double h = Math.Max(1, SvHost.ActualHeight);
        SvThumb.Margin = new Thickness(_s * w - 6, (1 - _v) * h - 6, 0, 0);
        HueThumb.Margin = new Thickness(0, (_h / 360.0) * Math.Max(0, h - 4), 0, 0);
    }
}
