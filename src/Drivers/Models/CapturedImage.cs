namespace Photobooth.Drivers.Models;

public record CapturedImage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Absolute path to the image or GIF file on disk.</summary>
    public string FilePath { get; init; } = string.Empty;

    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Overlay items the guest applied to this image.</summary>
    public IReadOnlyList<OverlayItem> Overlays { get; init; } = [];
}
