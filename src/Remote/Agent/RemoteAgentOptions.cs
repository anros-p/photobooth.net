namespace Photobooth.Remote.Agent;

public record RemoteAgentOptions
{
    /// <summary>WebSocket server URL, e.g. <c>wss://monitor.example.com/ws</c>.</summary>
    public string ServerUrl { get; init; } = string.Empty;

    /// <summary>Per-kiosk stable identifier sent in every heartbeat.</summary>
    public string KioskId { get; init; } = Environment.MachineName;

    /// <summary>API key sent in the <c>X-Api-Key</c> HTTP header during the WebSocket handshake.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>How often to send a heartbeat (default 10 s).</summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Initial reconnect delay; doubles on each attempt up to <see cref="MaxReconnectDelay"/>.</summary>
    public TimeSpan InitialReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan MaxReconnectDelay { get; init; } = TimeSpan.FromMinutes(2);
}
