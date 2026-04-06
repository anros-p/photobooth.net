namespace Photobooth.Remote.Protocol;

/// <summary>
/// A command sent from the server to the kiosk agent.
/// </summary>
public record RemoteCommand
{
    /// <summary>
    /// Discriminator. Known values:
    /// <list type="bullet">
    ///   <item><c>SetActiveEvent</c> — set the active event by ID (<see cref="Payload"/>)</item>
    ///   <item><c>PushEventConfig</c> — replace the active event config (<see cref="Payload"/> is JSON event object)</item>
    ///   <item><c>ResetToIdle</c> — navigate kiosk to Idle state</item>
    /// </list>
    /// </summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>Optional JSON payload; interpretation depends on <see cref="CommandType"/>.</summary>
    public string? Payload { get; init; }

    public string? CommandId { get; init; }
}
