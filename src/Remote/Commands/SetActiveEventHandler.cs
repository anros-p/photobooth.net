using Photobooth.Remote.Agent;
using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Commands;

/// <summary>
/// Handles the <c>SetActiveEvent</c> command.
/// The command payload is expected to be a string containing the event ID (GUID).
/// </summary>
public sealed class SetActiveEventHandler : ICommandHandler
{
    private readonly Func<Guid, CancellationToken, Task> _setActiveEvent;

    public string CommandType => "SetActiveEvent";

    /// <param name="setActiveEvent">
    /// Async callback that loads the event with the given ID from the store
    /// and activates it in the kiosk (e.g. update <c>KioskViewModel.ActiveEvent</c>).
    /// </param>
    public SetActiveEventHandler(Func<Guid, CancellationToken, Task> setActiveEvent)
    {
        _setActiveEvent = setActiveEvent;
    }

    public async Task<CommandAck> HandleAsync(RemoteCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Payload) ||
            !Guid.TryParse(command.Payload.Trim('"'), out var eventId))
        {
            return new CommandAck
            {
                CommandId = command.CommandId,
                Success = false,
                ErrorMessage = "Payload must be a valid event GUID."
            };
        }

        await _setActiveEvent(eventId, ct).ConfigureAwait(false);

        return new CommandAck { CommandId = command.CommandId, Success = true };
    }
}
