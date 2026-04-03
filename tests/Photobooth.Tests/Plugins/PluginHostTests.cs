using Photobooth.Drivers.Models;
using Photobooth.Plugins;

namespace Photobooth.Tests.Plugins;

public sealed class PluginHostTests
{
    private static Session MakeSession() => new()
    {
        EventId = Guid.NewGuid(),
        ComposedFilePath = "/tmp/photo.jpg"
    };

    // -----------------------------------------------------------------------
    // Registration
    // -----------------------------------------------------------------------

    [Fact]
    public void Plugins_ReflectsRegisteredBuiltIns()
    {
        var p1 = new FakePlugin("a");
        var p2 = new FakePlugin("b");
        var host = new PluginHost([p1, p2]);

        Assert.Equal(2, host.Plugins.Count);
    }

    [Fact]
    public void GetSharePlugins_ReturnsOnlyMatchingChannel()
    {
        var qr = new FakeSharePlugin("qr", ShareChannel.QrCode);
        var email = new FakeSharePlugin("email", ShareChannel.Email);
        var host = new PluginHost([qr, email]);

        var result = host.GetSharePlugins(ShareChannel.QrCode);

        Assert.Single(result);
        Assert.Equal("qr", result[0].Id);
    }

    [Fact]
    public void GetSharePlugins_NoMatch_ReturnsEmpty()
    {
        var host = new PluginHost([new FakeSharePlugin("qr", ShareChannel.QrCode)]);
        var result = host.GetSharePlugins(ShareChannel.Email);
        Assert.Empty(result);
    }

    [Fact]
    public void GetSharePlugins_MultiFlag_ReturnsAll()
    {
        var qr = new FakeSharePlugin("qr", ShareChannel.QrCode);
        var email = new FakeSharePlugin("email", ShareChannel.Email);
        var host = new PluginHost([qr, email]);

        var result = host.GetSharePlugins(ShareChannel.QrCode | ShareChannel.Email);
        Assert.Equal(2, result.Count);
    }

    // -----------------------------------------------------------------------
    // Dispatch
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DispatchSessionCompleted_CallsAllPlugins()
    {
        var p1 = new FakePlugin("a");
        var p2 = new FakePlugin("b");
        var host = new PluginHost([p1, p2]);

        await host.DispatchSessionCompletedAsync(MakeSession());

        Assert.Equal(1, p1.Invocations);
        Assert.Equal(1, p2.Invocations);
    }

    [Fact]
    public async Task DispatchSessionCompleted_FaultingPlugin_DoesNotBlockOthers()
    {
        var faulting = new ThrowingPlugin("bad");
        var good = new FakePlugin("good");
        var host = new PluginHost([faulting, good]);

        // Should not throw
        await host.DispatchSessionCompletedAsync(MakeSession());

        Assert.Equal(1, good.Invocations);
    }

    [Fact]
    public void EmptyPluginsDirectory_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var host = new PluginHost([], dir);
            Assert.Empty(host.Plugins);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void NonExistentPluginsDirectory_DoesNotThrow()
    {
        var host = new PluginHost([], "/does/not/exist");
        Assert.Empty(host.Plugins);
    }
}

// -----------------------------------------------------------------------
// Test doubles
// -----------------------------------------------------------------------

file sealed class FakePlugin(string id) : IPhotoboothPlugin
{
    public string Id => id;
    public string Name => id;
    public int Invocations { get; private set; }

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
    {
        Invocations++;
        return Task.CompletedTask;
    }
}

file sealed class FakeSharePlugin(string id, ShareChannel channel) : ISharePlugin
{
    public string Id => id;
    public string Name => id;
    public ShareChannel Channel => channel;

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default)
        => Task.FromResult(job with { Status = ShareStatus.Completed });
}

file sealed class ThrowingPlugin(string id) : IPhotoboothPlugin
{
    public string Id => id;
    public string Name => id;

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.FromException(new InvalidOperationException("Plugin crash!"));
}
