using Photobooth.Remote.Agent;
using Photobooth.Remote.Commands;
using Photobooth.Remote.Protocol;

namespace Photobooth.Tests.Remote;

public sealed class CommandDispatcherTests
{
    [Fact]
    public async Task Dispatch_UnknownCommand_ReturnsFailure()
    {
        var dispatcher = new CommandDispatcher([]);
        var ack = await dispatcher.DispatchAsync(new RemoteCommand { CommandType = "Unknown" });

        Assert.False(ack.Success);
        Assert.Contains("Unknown", ack.ErrorMessage);
    }

    [Fact]
    public async Task Dispatch_ResetToIdle_InvokesCallback()
    {
        var invoked = false;
        var handler = new ResetToIdleHandler(() => invoked = true);
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "ResetToIdle",
            CommandId = "r1"
        });

        Assert.True(ack.Success);
        Assert.Equal("r1", ack.CommandId);
        Assert.True(invoked);
    }

    [Fact]
    public async Task Dispatch_SetActiveEvent_ValidGuid_InvokesCallback()
    {
        var eventId = Guid.NewGuid();
        Guid? received = null;

        var handler = new SetActiveEventHandler((id, _) =>
        {
            received = id;
            return Task.CompletedTask;
        });
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "SetActiveEvent",
            Payload = $"\"{eventId}\""
        });

        Assert.True(ack.Success);
        Assert.Equal(eventId, received);
    }

    [Fact]
    public async Task Dispatch_SetActiveEvent_InvalidPayload_ReturnsFailure()
    {
        var handler = new SetActiveEventHandler((_, _) => Task.CompletedTask);
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "SetActiveEvent",
            Payload = "not-a-guid"
        });

        Assert.False(ack.Success);
    }

    [Fact]
    public async Task Dispatch_PushEventConfig_ValidJson_InvokesCallback()
    {
        string? received = null;

        var handler = new PushEventConfigHandler((doc, _) =>
        {
            received = doc.RootElement.GetProperty("name").GetString();
            return Task.CompletedTask;
        });
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "PushEventConfig",
            Payload = """{"name":"Test Event"}"""
        });

        Assert.True(ack.Success);
        Assert.Equal("Test Event", received);
    }

    [Fact]
    public async Task Dispatch_PushEventConfig_InvalidJson_ReturnsFailure()
    {
        var handler = new PushEventConfigHandler((_, _) => Task.CompletedTask);
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "PushEventConfig",
            Payload = "{invalid json"
        });

        Assert.False(ack.Success);
        Assert.Contains("Invalid JSON", ack.ErrorMessage);
    }

    [Fact]
    public async Task Dispatch_HandlerThrows_ReturnsFailureWithMessage()
    {
        var handler = new FaultyHandler();
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand { CommandType = "Faulty" });

        Assert.False(ack.Success);
        Assert.Contains("Boom", ack.ErrorMessage);
    }

    [Fact]
    public async Task Dispatch_PreservesCommandId()
    {
        var handler = new ResetToIdleHandler(() => { });
        var dispatcher = new CommandDispatcher([handler]);

        var ack = await dispatcher.DispatchAsync(new RemoteCommand
        {
            CommandType = "ResetToIdle",
            CommandId = "abc-123"
        });

        Assert.Equal("abc-123", ack.CommandId);
    }
}

file sealed class FaultyHandler : ICommandHandler
{
    public string CommandType => "Faulty";

    public Task<CommandAck> HandleAsync(RemoteCommand command, CancellationToken ct = default)
        => throw new InvalidOperationException("Boom");
}
