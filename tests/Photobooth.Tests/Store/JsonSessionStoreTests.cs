using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.Tests.Store;

public sealed class JsonSessionStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonSessionStore _store;

    public JsonSessionStoreTests()
    {
        _store = new JsonSessionStore(_tempDir);
    }

    [Fact]
    public async Task SaveAndGetById_RoundTripsAllFields()
    {
        var eventId = Guid.NewGuid();
        var session = new Session
        {
            EventId = eventId,
            ComposedFilePath = "/data/sessions/abc.jpg",
            Captures =
            [
                new CapturedImage
                {
                    FilePath = "/data/captures/1.jpg",
                    Overlays =
                    [
                        new OverlayItem { AssetId = Guid.NewGuid(), X = 10, Y = 20, Width = 100, Height = 100, Rotation = 45 }
                    ]
                }
            ]
        };

        await _store.SaveAsync(session);
        var loaded = await _store.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);
        Assert.Equal(session.EventId, loaded.EventId);
        Assert.Equal(session.ComposedFilePath, loaded.ComposedFilePath);
        Assert.Single(loaded.Captures);
        Assert.Single(loaded.Captures[0].Overlays);
        Assert.Equal(45, loaded.Captures[0].Overlays[0].Rotation);
    }

    [Fact]
    public async Task GetByEvent_ReturnsOnlyMatchingSessions()
    {
        var eventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();

        await _store.SaveAsync(new Session { EventId = eventId });
        await _store.SaveAsync(new Session { EventId = eventId });
        await _store.SaveAsync(new Session { EventId = otherEventId });

        var sessions = await _store.GetByEventAsync(eventId);

        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, s => Assert.Equal(eventId, s.EventId));
    }

    [Fact]
    public async Task Delete_RemovesSession()
    {
        var session = new Session { EventId = Guid.NewGuid() };
        await _store.SaveAsync(session);

        await _store.DeleteAsync(session.Id);

        var loaded = await _store.GetByIdAsync(session.Id);
        Assert.Null(loaded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
