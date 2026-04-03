using Photobooth.Drivers.Models;
using Photobooth.Plugins.Upload;

namespace Photobooth.Tests.Plugins;

public sealed class UploadQueueTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"uq_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private UploadQueue MakeQueue(IUploadTransport? transport = null, int maxAttempts = 3) =>
        new(_dir, transport ?? new FakeTransport(), maxAttempts);

    private static ShareJob MakeJob(string? filePath = null) => new()
    {
        SessionId = Guid.NewGuid(),
        Channel = ShareChannel.QrCode,
        FilePath = filePath ?? string.Empty
    };

    // -----------------------------------------------------------------------
    // Success
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_Success_ReturnsCompletedJob()
    {
        var queue = MakeQueue(new FakeTransport("https://example.com/photo.jpg"));
        var job = await queue.EnqueueAsync(MakeJob());

        Assert.Equal(ShareStatus.Completed, job.Status);
        Assert.Equal("https://example.com/photo.jpg", job.PublicUrl);
    }

    [Fact]
    public async Task Enqueue_Success_IncreasesCompletedCount()
    {
        var queue = MakeQueue();
        await queue.EnqueueAsync(MakeJob());
        await queue.EnqueueAsync(MakeJob());

        Assert.Equal(2, await queue.GetCompletedCountAsync());
    }

    // -----------------------------------------------------------------------
    // Failure / retry
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_Fails_StatusIsQueued_WhenAttemptsRemain()
    {
        var queue = MakeQueue(new FailingTransport(), maxAttempts: 3);
        var job = await queue.EnqueueAsync(MakeJob());

        Assert.Equal(ShareStatus.Queued, job.Status);
    }

    [Fact]
    public async Task Enqueue_ExhaustsAttempts_StatusIsFailed()
    {
        var queue = MakeQueue(new FailingTransport(), maxAttempts: 1);
        var job = await queue.EnqueueAsync(MakeJob());

        Assert.Equal(ShareStatus.Failed, job.Status);
    }

    [Fact]
    public async Task RetryPending_SucceedsOnRetry_StatusBecomesCompleted()
    {
        var transport = new CountdownTransport(failuresBeforeSuccess: 1);
        var queue = MakeQueue(transport, maxAttempts: 3);

        await queue.EnqueueAsync(MakeJob()); // fails → Queued
        await queue.RetryPendingAsync();     // succeeds

        Assert.Equal(1, await queue.GetCompletedCountAsync());
    }

    // -----------------------------------------------------------------------
    // MaxAttempts property
    // -----------------------------------------------------------------------

    [Fact]
    public void MaxAttempts_ReflectsConstructorValue()
    {
        Assert.Equal(5, MakeQueue(maxAttempts: 5).MaxAttempts);
    }
}

// -----------------------------------------------------------------------
// Test doubles
// -----------------------------------------------------------------------

file sealed class FakeTransport(string? url = null) : IUploadTransport
{
    public Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        => Task.FromResult(url);
}

file sealed class FailingTransport : IUploadTransport
{
    public Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        => Task.FromException<string?>(new HttpRequestException("Network error."));
}

file sealed class CountdownTransport(int failuresBeforeSuccess) : IUploadTransport
{
    private int _calls;

    public Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
    {
        if (++_calls <= failuresBeforeSuccess)
            return Task.FromException<string?>(new HttpRequestException("Network error."));
        return Task.FromResult<string?>(null);
    }
}
