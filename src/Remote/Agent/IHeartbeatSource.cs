using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Agent;

/// <summary>
/// Supplies the current kiosk status for inclusion in heartbeat messages.
/// Implement this in the App layer to expose live metrics to the remote agent.
/// </summary>
public interface IHeartbeatSource
{
    Task<HeartbeatMessage> GetHeartbeatAsync(CancellationToken ct = default);
}
