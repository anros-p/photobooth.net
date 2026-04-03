namespace Photobooth.Drivers.Models;

/// <summary>
/// Defines the position, size, and rotation of a single photo within a layout template.
/// Coordinates are in pixels relative to the layout canvas origin (top-left).
/// </summary>
public record PhotoSlot
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>X position of the top-left corner in pixels.</summary>
    public double X { get; init; }

    /// <summary>Y position of the top-left corner in pixels.</summary>
    public double Y { get; init; }

    public double Width { get; init; }
    public double Height { get; init; }

    /// <summary>Rotation in degrees, clockwise.</summary>
    public double Rotation { get; init; }
}
