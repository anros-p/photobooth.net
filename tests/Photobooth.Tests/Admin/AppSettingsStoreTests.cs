using Photobooth.App.Admin;

namespace Photobooth.Tests.Admin;

public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"cfg_{Guid.NewGuid():N}");

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task Load_NoFile_ReturnsDefaults()
    {
        var store = new AppSettingsStore(_dir);
        var settings = await store.LoadAsync();
        Assert.Equal("auto", settings.CameraDriverOverride);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var store = new AppSettingsStore(_dir);
        var saved = new AppSettings
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 465,
            CameraDriverOverride = "simulated"
        };

        await store.SaveAsync(saved);
        var loaded = await store.LoadAsync();

        Assert.Equal("smtp.example.com", loaded.SmtpHost);
        Assert.Equal(465, loaded.SmtpPort);
        Assert.Equal("simulated", loaded.CameraDriverOverride);
    }
}
