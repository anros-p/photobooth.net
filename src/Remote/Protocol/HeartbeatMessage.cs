namespace Photobooth.Remote.Protocol;

/// <summary>
/// Status heartbeat sent from the kiosk agent to the server every 10 seconds.
/// </summary>
public record HeartbeatMessage
{
    public string MessageType { get; init; } = "heartbeat";
    public string KioskId { get; init; } = string.Empty;
    public string? ActiveEventId { get; init; }
    public string? ActiveEventName { get; init; }
    public int SessionCount { get; init; }
    public int PrintCount { get; init; }
    public string CameraStatus { get; init; } = "unknown";
    public string PrinterStatus { get; init; } = "unknown";
    public long DiskFreeBytes { get; init; }
    public int UploadQueueDepth { get; init; }

    /// <summary>Base64-encoded JPEG thumbnail of the last composited photo, or null.</summary>
    public string? LastPhotoThumbnailBase64 { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
