namespace Photobooth.Drivers.Models;

public record ShareJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public ShareChannel Channel { get; init; }

    /// <summary>Recipient address: email address, phone number, or empty for QR/microsite.</summary>
    public string Recipient { get; init; } = string.Empty;

    public ShareStatus Status { get; init; } = ShareStatus.Queued;
    public DateTimeOffset QueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>Public URL returned by the hosting endpoint after upload.</summary>
    public string? PublicUrl { get; init; }
}
