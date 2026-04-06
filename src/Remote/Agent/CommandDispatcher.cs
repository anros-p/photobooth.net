using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Agent;

/// <summary>
/// Routes inbound <see cref="RemoteCommand"/> messages to the appropriate <see cref="ICommandHandler"/>.
/// Unknown command types are acknowledged with a failure response (no exception thrown).
/// </summary>
public sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandDispatcher(IEnumerable<ICommandHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.CommandType, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<CommandAck> DispatchAsync(RemoteCommand command, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(command.CommandType, out var handler))
        {
            try
            {
                return await handler.HandleAsync(command, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Ack(command, success: false, error: ex.Message);
            }
        }

        return Ack(command, success: false,
            error: $"Unknown command type '{command.CommandType}'.");
    }

    private static CommandAck Ack(RemoteCommand cmd, bool success, string? error = null) =>
        new() { CommandId = cmd.CommandId, Success = success, ErrorMessage = error };
}
