namespace ICY.Models;

/// <summary>
/// One OS-reported timing from EnumDisplaySettingsEx.
/// Not associated with any application or game.
/// </summary>
public sealed class DisplayModeOption : IEquatable<DisplayModeOption>
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int RefreshHz { get; init; }
    public required int BitsPerPixel { get; init; }

    public string Label => $"{Width} × {Height}    {RefreshHz} Hz    {BitsPerPixel}-bit";

    public string ShortLabel => $"{Width} × {Height} @ {RefreshHz} Hz";

    public bool Equals(DisplayModeOption? other)
        => other is not null
           && Width == other.Width
           && Height == other.Height
           && RefreshHz == other.RefreshHz
           && BitsPerPixel == other.BitsPerPixel;

    public override bool Equals(object? obj) => Equals(obj as DisplayModeOption);

    public override int GetHashCode() => HashCode.Combine(Width, Height, RefreshHz, BitsPerPixel);

    public override string ToString() => Label;
}
