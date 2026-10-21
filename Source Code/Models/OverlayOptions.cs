using System.Collections.ObjectModel;
using System.Windows.Media;
using ICY.Infrastructure;

namespace ICY.Models;

public enum CrosshairStyle
{
    Cross,
    Dot,
    Circle,
    CrossAndDot,
    TShape,
    X,
    Custom
}

public enum OverlayPosition
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum OverlayHotkeyMode
{
    Hold,
    Toggle
}

/// <summary>
/// Visual options for ICY's own overlay window.
/// The overlay is a static graphic. It never reads pixels, game state, or input.
/// </summary>
public sealed class OverlayOptions : ObservableObject
{
    private bool _enabled;
    private CrosshairStyle _style = CrosshairStyle.Cross;
    private Color _color = Color.FromRgb(0x7E, 0xC8, 0xE3);
    private double _size = 16;
    private double _thickness = 2;
    private double _gap = 4;
    private double _opacity = 0.95;
    private double _offsetX;
    private double _offsetY;
    private double _savedOffsetX;
    private double _savedOffsetY;
    private bool _dotCenter;
    private double _dotSize = 3;
    private OverlayPosition _position = OverlayPosition.Center;
    private string? _customImagePath;
    private int _hotkeyVk;
    private OverlayHotkeyMode _hotkeyMode = OverlayHotkeyMode.Hold;

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public CrosshairStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (SetField(ref _color, value))
            {
                OnPropertyChanged(nameof(ColorHex));
            }
        }
    }

    public string ColorHex
    {
        get => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
        set
        {
            if (TryParseHex(value, out var color))
            {
                Color = color;
            }
        }
    }

    public double Size
    {
        get => _size;
        set => SetClamped(ref _size, value, 4, 80);
    }

    public double Thickness
    {
        get => _thickness;
        set => SetClamped(ref _thickness, value, 1, 8);
    }

    public double Gap
    {
        get => _gap;
        set => SetClamped(ref _gap, value, 0, 32);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetClamped(ref _opacity, value, 0.15, 1.0);
    }

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (SetClamped(ref _offsetX, value, -4000, 4000))
            {
                OnPropertyChanged(nameof(PositionText));
            }
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (SetClamped(ref _offsetY, value, -4000, 4000))
            {
                OnPropertyChanged(nameof(PositionText));
            }
        }
    }

    public double SavedOffsetX
    {
        get => _savedOffsetX;
        set => SetClamped(ref _savedOffsetX, value, -4000, 4000);
    }

    public double SavedOffsetY
    {
        get => _savedOffsetY;
        set => SetClamped(ref _savedOffsetY, value, -4000, 4000);
    }

    public string PositionText
    {
        get => $"{(int)Math.Round(OffsetX)},{(int)Math.Round(OffsetY)}";
        set
        {
            if (!TryParsePoint(value, out double x, out double y))
            {
                return;
            }

            OffsetX = x;
            OffsetY = y;
        }
    }

    public void Nudge(int dx, int dy)
    {
        OffsetX += dx;
        OffsetY += dy;
    }

    public ObservableCollection<SavedOverlayPosition> SavedPositions { get; } = [];

    public void ResetLook()
    {
        Style = CrosshairStyle.Cross;
        Color = Color.FromRgb(0x7E, 0xC8, 0xE3);
        Size = 16;
        Thickness = 2;
        Gap = 4;
        Opacity = 0.95;
        DotCenter = false;
        DotSize = 3;
        CustomImagePath = null;
    }

    private bool SetClamped(ref double field, double value, double min, double max, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        double next = Math.Round(Math.Clamp(value, min, max), 0);
        if (Math.Abs(field - next) < 0.0005)
        {
            return false;
        }

        field = next;
        OnPropertyChanged(name);
        return true;
    }

    public static bool TryParsePoint(string? text, out double x, out double y)
    {
        x = 0;
        y = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string[] parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        return double.TryParse(parts[0], out x) && double.TryParse(parts[1], out y);
    }

    public bool DotCenter
    {
        get => _dotCenter;
        set => SetField(ref _dotCenter, value);
    }

    public double DotSize
    {
        get => _dotSize;
        set => SetClamped(ref _dotSize, value, 1, 24);
    }

    public OverlayPosition Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    public string? CustomImagePath
    {
        get => _customImagePath;
        set => SetField(ref _customImagePath, value);
    }

    public int HotkeyVk
    {
        get => _hotkeyVk;
        set
        {
            if (SetField(ref _hotkeyVk, value))
            {
                OnPropertyChanged(nameof(HotkeyLabel));
            }
        }
    }

    public OverlayHotkeyMode HotkeyMode
    {
        get => _hotkeyMode;
        set => SetField(ref _hotkeyMode, value);
    }

    public string HotkeyLabel => HotkeyVk == 0 ? "None" : HotkeyNames.Describe(HotkeyVk);

    public bool UseCustomImage =>
        Style == CrosshairStyle.Custom || !string.IsNullOrWhiteSpace(CustomImagePath);

    public void CopyFrom(OverlayOptions other)
    {
        Enabled = other.Enabled;
        Style = other.Style;
        Color = other.Color;
        Size = other.Size;
        Thickness = other.Thickness;
        Gap = other.Gap;
        Opacity = other.Opacity;
        OffsetX = other.OffsetX;
        OffsetY = other.OffsetY;
        SavedOffsetX = other.SavedOffsetX;
        SavedOffsetY = other.SavedOffsetY;
        DotCenter = other.DotCenter;
        DotSize = other.DotSize;
        Position = other.Position;
        CustomImagePath = other.CustomImagePath;
        HotkeyVk = other.HotkeyVk;
        HotkeyMode = other.HotkeyMode;
        SavedPositions.Clear();
        foreach (var item in other.SavedPositions)
        {
            SavedPositions.Add(new SavedOverlayPosition
            {
                Name = item.Name,
                X = item.X,
                Y = item.Y
            });
        }
    }

    public static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (text.Length != 6)
        {
            return false;
        }

        try
        {
            byte r = Convert.ToByte(text[..2], 16);
            byte g = Convert.ToByte(text[2..4], 16);
            byte b = Convert.ToByte(text[4..6], 16);
            color = Color.FromRgb(r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
