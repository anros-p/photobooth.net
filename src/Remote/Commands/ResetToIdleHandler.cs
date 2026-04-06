using Photobooth.Remote.Agent;
using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Commands;

/// <summary>
/// Handles the <c>ResetToIdle</c> command by invoking a provided callback that
/// navigates the kiosk to its Idle state.
/// </summary>
public sealed class ResetToIdleHandler : ICommandHandler
{
    private readonly Action _resetAction;

    public string CommandType => "ResetToIdle";

    /// <param name="resetAction">
    /// Callback invoked on the UI/service layer (e.g. <c>KioskNavigator.Reset()</c>).
    /// </param>
    public ResetToIdleHandler(Action resetAction)
    {
        _resetAction = resetAction;
    }

    public Task<CommandAck> HandleAsync(RemoteCommand command, CancellationToken ct = default)
    {
        _resetAction();
        return Task.FromResult(new CommandAck
        {
            CommandId = command.CommandId,
            Success = true
        });
    }
}
