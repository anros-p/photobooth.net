namespace Photobooth.Drivers.Models;

public record OverlayAsset
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    /// <summary>Path to the PNG asset file.</summary>
    public string FilePath { get; init; } = string.Empty;
}
