namespace Photobooth.Drivers.Models;

public record Session
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid EventId { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Individual captures (stills or GIF source frames).</summary>
    public IReadOnlyList<CapturedImage> Captures { get; init; } = [];

    /// <summary>Path to the final composited image or GIF file.</summary>
    public string ComposedFilePath { get; init; } = string.Empty;
}
