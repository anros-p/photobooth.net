namespace Photobooth.Drivers.Models;

/// <summary>
/// An instance of an overlay asset placed by a guest on their photo.
/// Position and size are relative to the composition canvas.
/// </summary>
public record OverlayItem
{
    public Guid AssetId { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    /// <summary>Rotation in degrees, clockwise.</summary>
    public double Rotation { get; init; }
}
