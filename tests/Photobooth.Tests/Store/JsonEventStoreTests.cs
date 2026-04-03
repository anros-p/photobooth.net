using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.Tests.Store;

public sealed class JsonEventStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly JsonEventStore _store;

    public JsonEventStoreTests()
    {
        _store = new JsonEventStore(_tempDir);
    }

    [Fact]
    public async Task SaveAndGetById_RoundTripsAllFields()
    {
        var evt = new Event
        {
            Name = "Test Event",
            CaptureMode = CaptureMode.Gif,
            CountdownSeconds = 5,
            PrintingEnabled = true,
            MaxPrints = 100,
            DefaultLanguage = "fr",
            AvailableLanguages = ["en", "fr"],
            EnabledShareChannels = ShareChannel.Email | ShareChannel.QrCode,
            Layout = new LayoutTemplate
            {
                Name = "4x6 Strip",
                CanvasWidth = 1800,
                CanvasHeight = 1200,
                PhotoSlots =
                [
                    new PhotoSlot { X = 10, Y = 10, Width = 400, Height = 300, Rotation = 5.5 },
                    new PhotoSlot { X = 420, Y = 10, Width = 400, Height = 300, Rotation = 0 }
                ]
            },
            MicrositeBranding = new MicrositeBranding
            {
                EventName = "Wedding 2026",
                PrimaryColour = "#FF5733"
            }
        };

        await _store.SaveAsync(evt);
        var loaded = await _store.GetByIdAsync(evt.Id);

        Assert.NotNull(loaded);
        Assert.Equal(evt.Name, loaded.Name);
        Assert.Equal(evt.CaptureMode, loaded.CaptureMode);
        Assert.Equal(evt.CountdownSeconds, loaded.CountdownSeconds);
        Assert.Equal(evt.PrintingEnabled, loaded.PrintingEnabled);
        Assert.Equal(evt.MaxPrints, loaded.MaxPrints);
        Assert.Equal(evt.DefaultLanguage, loaded.DefaultLanguage);
        Assert.Equal(evt.AvailableLanguages, loaded.AvailableLanguages);
        Assert.Equal(evt.EnabledShareChannels, loaded.EnabledShareChannels);
        Assert.Equal(evt.Layout!.Name, loaded.Layout!.Name);
        Assert.Equal(evt.Layout.PhotoSlots.Count, loaded.Layout.PhotoSlots.Count);
        Assert.Equal(evt.Layout.PhotoSlots[0].Rotation, loaded.Layout.PhotoSlots[0].Rotation);
        Assert.Equal(evt.MicrositeBranding!.PrimaryColour, loaded.MicrositeBranding!.PrimaryColour);
    }

    [Fact]
    public async Task GetAll_ReturnsAllSavedEvents()
    {
        var evt1 = new Event { Name = "Alpha" };
        var evt2 = new Event { Name = "Beta" };

        await _store.SaveAsync(evt1);
        await _store.SaveAsync(evt2);

        var all = await _store.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, e => e.Name == "Alpha");
        Assert.Contains(all, e => e.Name == "Beta");
    }

    [Fact]
    public async Task Delete_RemovesEvent()
    {
        var evt = new Event { Name = "ToDelete" };
        await _store.SaveAsync(evt);

        await _store.DeleteAsync(evt.Id);

        var loaded = await _store.GetByIdAsync(evt.Id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetById_ReturnsNullForUnknownId()
    {
        var result = await _store.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
