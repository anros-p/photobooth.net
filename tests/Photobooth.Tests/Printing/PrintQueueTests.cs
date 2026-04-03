using Photobooth.Printing.Print;

namespace Photobooth.Tests.Printing;

public sealed class PrintQueueTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"pq_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private PrintQueue MakeQueue(IPrintService? service = null, int maxAttempts = 3) =>
        new(_dir, service ?? new FakePrintService(), maxAttempts);

    private static string WriteTempImage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");
        // Minimal 1×1 JPEG
        File.WriteAllBytes(path, MinimalJpeg);
        return path;
    }

    // 1×1 white JPEG — smallest valid JPEG header that passes File.Exists
    private static readonly byte[] MinimalJpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
        0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
        0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
        0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
        0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
        0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
        0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
        0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
        0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
        0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0xFB, 0xFF,
        0xD9
    ];

    // -----------------------------------------------------------------------
    // Enqueue / complete
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_SuccessfulPrint_ReturnsCompletedJob()
    {
        var imagePath = WriteTempImage();
        try
        {
            var queue = MakeQueue();
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };
            var job = await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts, 0, 0);

            Assert.Equal(PrintStatus.Completed, job.Status);
        }
        finally { File.Delete(imagePath); }
    }

    [Fact]
    public async Task Enqueue_SuccessfulPrint_IncreasesCompletedCount()
    {
        var imagePath = WriteTempImage();
        try
        {
            var queue = MakeQueue();
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 2 };
            await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts, 0, 0);

            var count = await queue.GetCompletedCountAsync();
            Assert.Equal(2, count); // Copies = 2
        }
        finally { File.Delete(imagePath); }
    }

    // -----------------------------------------------------------------------
    // Max prints limit
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_ExceedsMaxPrints_Throws()
    {
        var imagePath = WriteTempImage();
        try
        {
            var queue = MakeQueue();
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts,
                    eventPrintCount: 5, maxPrints: 5));
        }
        finally { File.Delete(imagePath); }
    }

    [Fact]
    public async Task Enqueue_MaxPrintsZero_DoesNotThrow()
    {
        var imagePath = WriteTempImage();
        try
        {
            var queue = MakeQueue();
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };
            // maxPrints = 0 means unlimited
            var job = await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts,
                eventPrintCount: 999, maxPrints: 0);

            Assert.Equal(PrintStatus.Completed, job.Status);
        }
        finally { File.Delete(imagePath); }
    }

    // -----------------------------------------------------------------------
    // Failure / retry
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Enqueue_PrintFails_StatusIsQueued()
    {
        var imagePath = WriteTempImage();
        try
        {
            // maxAttempts=3; after first failure with 1 attempt remaining → Queued
            var queue = MakeQueue(new FailingPrintService(), maxAttempts: 3);
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };
            var job = await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts, 0, 0);

            Assert.Equal(PrintStatus.Queued, job.Status);
        }
        finally { File.Delete(imagePath); }
    }

    [Fact]
    public async Task Enqueue_PrintFailsAllAttempts_StatusIsFailed()
    {
        var imagePath = WriteTempImage();
        try
        {
            // maxAttempts=1; one failure → Failed
            var queue = MakeQueue(new FailingPrintService(), maxAttempts: 1);
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };
            var job = await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts, 0, 0);

            Assert.Equal(PrintStatus.Failed, job.Status);
        }
        finally { File.Delete(imagePath); }
    }

    [Fact]
    public async Task RetryPending_SucceedsOnRetry_StatusBecomesCompleted()
    {
        var imagePath = WriteTempImage();
        try
        {
            var fakeService = new CountdownPrintService(failuresBeforeSuccess: 1);
            var queue = MakeQueue(fakeService, maxAttempts: 3);
            var opts = new PrintOptions { PrinterName = "TestPrinter", Copies = 1 };

            // First attempt → fails → Queued
            await queue.EnqueueAsync(Guid.NewGuid(), imagePath, opts, 0, 0);

            // Retry → succeeds
            await queue.RetryPendingAsync();

            var count = await queue.GetCompletedCountAsync();
            Assert.Equal(1, count);
        }
        finally { File.Delete(imagePath); }
    }

    // -----------------------------------------------------------------------
    // MaxAttempts property
    // -----------------------------------------------------------------------

    [Fact]
    public void MaxAttempts_ReflectsConstructorValue()
    {
        var queue = MakeQueue(maxAttempts: 7);
        Assert.Equal(7, queue.MaxAttempts);
    }
}

// -----------------------------------------------------------------------
// Test doubles
// -----------------------------------------------------------------------

file sealed class FakePrintService : IPrintService
{
    public Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([new PrinterInfo { Name = "Fake" }]);

    public Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class FailingPrintService : IPrintService
{
    public Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([]);

    public Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default)
        => Task.FromException(new InvalidOperationException("Printer offline."));
}

/// <summary>Fails the first <paramref name="failuresBeforeSuccess"/> calls, then succeeds.</summary>
file sealed class CountdownPrintService(int failuresBeforeSuccess) : IPrintService
{
    private int _calls;

    public Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PrinterInfo>>([]);

    public Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default)
    {
        if (++_calls <= failuresBeforeSuccess)
            return Task.FromException(new InvalidOperationException("Printer offline."));
        return Task.CompletedTask;
    }
}
