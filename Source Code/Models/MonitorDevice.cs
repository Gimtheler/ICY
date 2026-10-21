using ICY.Infrastructure;

namespace ICY.Models;

/// <summary>
/// A connected display as reported by EnumDisplayDevices.
/// Device names are adapter identifiers (for example \\.\DISPLAY1), not window titles.
/// </summary>
public sealed class MonitorDevice : ObservableObject
{
    private bool _isSelected;

    public required string DeviceName { get; init; }
    public required string AdapterName { get; init; }
    public required string FriendlyName { get; init; }
    public required bool IsPrimary { get; init; }
    public required int PixelLeft { get; init; }
    public required int PixelTop { get; init; }
    public required int PixelWidth { get; init; }
    public required int PixelHeight { get; init; }
    public required uint DpiX { get; init; }
    public required uint DpiY { get; init; }
    public int Number { get; set; } = 1;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string DisplayLabel =>
        IsPrimary
            ? $"{Number}  ·  {FriendlyName}  ·  Primary"
            : $"{Number}  ·  {FriendlyName}";

    public string BoundsLabel => $"{PixelWidth} × {PixelHeight}";

    public override string ToString() => DisplayLabel;
}
