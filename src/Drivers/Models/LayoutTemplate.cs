namespace Photobooth.Drivers.Models;

public record LayoutTemplate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;

    /// <summary>Absolute path or relative path to the background image file.</summary>
    public string BackgroundImagePath { get; init; } = string.Empty;

    /// <summary>Canvas width in pixels (defines the composition coordinate space).</summary>
    public double CanvasWidth { get; init; }

    /// <summary>Canvas height in pixels.</summary>
    public double CanvasHeight { get; init; }

    public IReadOnlyList<PhotoSlot> PhotoSlots { get; init; } = [];
}
