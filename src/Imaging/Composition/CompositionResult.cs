namespace Photobooth.Imaging.Composition;

public record CompositionResult
{
    /// <summary>Absolute path to the composited output image.</summary>
    public required string OutputPath { get; init; }

    public int WidthPixels { get; init; }
    public int HeightPixels { get; init; }
    public DateTimeOffset ComposedAt { get; init; } = DateTimeOffset.UtcNow;
}
