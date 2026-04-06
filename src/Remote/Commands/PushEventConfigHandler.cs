using System.Text.Json;
using Photobooth.Remote.Agent;
using Photobooth.Remote.Protocol;

namespace Photobooth.Remote.Commands;

/// <summary>
/// Handles the <c>PushEventConfig</c> command.
/// The payload is a JSON-serialised event configuration object.
/// The handler deserialises it and passes it to the provided callback.
/// </summary>
public sealed class PushEventConfigHandler : ICommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly Func<JsonDocument, CancellationToken, Task> _applyConfig;

    public string CommandType => "PushEventConfig";

    /// <param name="applyConfig">
    /// Callback that receives the raw JSON document and applies it to the active event.
    /// Using <see cref="JsonDocument"/> keeps the Remote project free of a dependency on Drivers.
    /// </param>
    public PushEventConfigHandler(Func<JsonDocument, CancellationToken, Task> applyConfig)
    {
        _applyConfig = applyConfig;
    }

    public async Task<CommandAck> HandleAsync(RemoteCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Payload))
        {
            return new CommandAck
            {
                CommandId = command.CommandId,
                Success = false,
                ErrorMessage = "Payload is empty."
            };
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(command.Payload); }
        catch (JsonException ex)
        {
            return new CommandAck
            {
                CommandId = command.CommandId,
                Success = false,
                ErrorMessage = $"Invalid JSON payload: {ex.Message}"
            };
        }

        await _applyConfig(doc, ct).ConfigureAwait(false);
        return new CommandAck { CommandId = command.CommandId, Success = true };
    }
}
