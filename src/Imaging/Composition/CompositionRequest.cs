using Photobooth.Drivers.Models;

namespace Photobooth.Imaging.Composition;

/// <summary>
/// Everything the compositor needs to produce a final image.
/// </summary>
public record CompositionRequest
{
    /// <summary>Layout template defining the canvas, background, and photo slots.</summary>
    public required LayoutTemplate Layout { get; init; }

    /// <summary>
    /// Captured image file paths in slot order.
    /// Count must match <see cref="LayoutTemplate.PhotoSlots"/>.
    /// </summary>
    public required IReadOnlyList<string> CaptureFilePaths { get; init; }

    /// <summary>Overlay items placed by the guest (may be empty).</summary>
    public IReadOnlyList<OverlayPlacement> Overlays { get; init; } = [];

    /// <summary>JPEG quality 1–100 for the output file (default 92).</summary>
    public int OutputQuality { get; init; } = 92;
}

/// <summary>
/// A single overlay asset placed on the canvas at a given position.
/// </summary>
public record OverlayPlacement
{
    /// <summary>Absolute path to the PNG overlay asset.</summary>
    public required string AssetPath { get; init; }

    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    /// <summary>Rotation in degrees, clockwise.</summary>
    public double Rotation { get; init; }
}
