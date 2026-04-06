using Photobooth.Remote.Protocol;

namespace Photobooth.Tests.Remote;

public sealed class ProtocolSerializerTests
{
    [Fact]
    public void Serialize_Heartbeat_RoundTrips()
    {
        var msg = new HeartbeatMessage
        {
            KioskId = "kiosk-1",
            ActiveEventId = Guid.NewGuid().ToString(),
            SessionCount = 5,
            CameraStatus = "connected"
        };

        var bytes = ProtocolSerializer.Serialize(msg);
        var result = ProtocolSerializer.Deserialize<HeartbeatMessage>(bytes);

        Assert.NotNull(result);
        Assert.Equal("kiosk-1", result!.KioskId);
        Assert.Equal(5, result.SessionCount);
        Assert.Equal("connected", result.CameraStatus);
    }

    [Fact]
    public void Serialize_Command_RoundTrips()
    {
        var cmd = new RemoteCommand
        {
            CommandType = "ResetToIdle",
            CommandId = "cmd-001"
        };

        var bytes = ProtocolSerializer.Serialize(cmd);
        var result = ProtocolSerializer.Deserialize<RemoteCommand>(bytes);

        Assert.NotNull(result);
        Assert.Equal("ResetToIdle", result!.CommandType);
        Assert.Equal("cmd-001", result.CommandId);
    }

    [Fact]
    public void ReadType_Heartbeat_ReturnsMessageType()
    {
        var bytes = ProtocolSerializer.Serialize(new HeartbeatMessage());
        Assert.Equal("heartbeat", ProtocolSerializer.ReadType(bytes));
    }

    [Fact]
    public void ReadType_Command_ReturnsCommandType()
    {
        var bytes = ProtocolSerializer.Serialize(new RemoteCommand { CommandType = "SetActiveEvent" });
        Assert.Equal("SetActiveEvent", ProtocolSerializer.ReadType(bytes));
    }

    [Fact]
    public void ReadType_InvalidJson_ReturnsNull()
    {
        Assert.Null(ProtocolSerializer.ReadType("not json"u8));
    }
}
