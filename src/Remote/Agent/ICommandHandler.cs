using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Agent;

/// <summary>
/// Handles a specific inbound command type.
/// Implementations are registered with <see cref="CommandDispatcher"/>.
/// </summary>
public interface ICommandHandler
{
    string CommandType { get; }
    Task<CommandAck> HandleAsync(RemoteCommand command, CancellationToken ct = default);
}
