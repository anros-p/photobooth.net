namespace Photobooth.Remote.Protocol;

/// <summary>Acknowledgement sent back to the server after a command is processed.</summary>
public record CommandAck
{
    public string MessageType { get; init; } = "ack";
    public string? CommandId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
