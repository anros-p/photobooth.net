using System.Text.Json;

namespace Photobooth.Printing.Print;

/// <summary>
/// A persistent, retry-capable print queue backed by JSON files on disk.
/// Jobs survive application restarts. Failed jobs are retried up to
/// <see cref="MaxAttempts"/> times.
/// </summary>
public sealed class PrintQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;
    private readonly IPrintService _printService;
    private readonly int _maxAttempts;

    public int MaxAttempts => _maxAttempts;

    public PrintQueue(string dataDirectory, IPrintService printService, int maxAttempts = 3)
    {
        _directory = Path.Combine(dataDirectory, "print_queue");
        _printService = printService;
        _maxAttempts = maxAttempts;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Enqueues a new print job and immediately attempts to process it.</summary>
    /// <returns>The enqueued <see cref="PrintJob"/>.</returns>
    public async Task<PrintJob> EnqueueAsync(
        Guid sessionId, string imagePath, PrintOptions options,
        int eventPrintCount, int maxPrints,
        CancellationToken ct = default)
    {
        if (maxPrints > 0 && eventPrintCount >= maxPrints)
            throw new InvalidOperationException(
                $"Print limit of {maxPrints} reached for this event.");

        var job = new PrintJob
        {
            SessionId = sessionId,
            PrinterName = options.PrinterName,
            MediaSize = options.MediaSize,
            Copies = options.Copies,
            Status = PrintStatus.Queued
        };

        await SaveJobAsync(job, imagePath).ConfigureAwait(false);
        return await ProcessJobAsync(job, imagePath, ct).ConfigureAwait(false);
    }

    /// <summary>Retries all queued or failed jobs (e.g. after printer comes back online).</summary>
    public async Task RetryPendingAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var entry = await LoadEntryAsync(file, ct).ConfigureAwait(false);
            if (entry is null) continue;

            if (entry.Job.Status is PrintStatus.Queued or PrintStatus.Failed
                && entry.Attempts < _maxAttempts)
            {
                await ProcessJobAsync(entry.Job, entry.ImagePath, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Returns total completed print count across all jobs in the queue.</summary>
    public async Task<int> GetCompletedCountAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        var count = 0;
        foreach (var file in files)
        {
            var entry = await LoadEntryAsync(file, ct).ConfigureAwait(false);
            if (entry?.Job.Status == PrintStatus.Completed)
                count += entry.Job.Copies;
        }
        return count;
    }

    private async Task<PrintJob> ProcessJobAsync(PrintJob job, string imagePath, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(JobFilePath(job.Id), ct).ConfigureAwait(false)
                    ?? new QueueEntry { Job = job, ImagePath = imagePath };

        entry = entry with
        {
            Job = entry.Job with { Status = PrintStatus.Printing },
            Attempts = entry.Attempts + 1
        };
        await PersistEntryAsync(entry).ConfigureAwait(false);

        try
        {
            var opts = new PrintOptions
            {
                PrinterName = job.PrinterName,
                MediaSize = job.MediaSize,
                Copies = job.Copies
            };

            await _printService.PrintAsync(imagePath, opts, ct).ConfigureAwait(false);

            entry = entry with
            {
                Job = entry.Job with
                {
                    Status = PrintStatus.Completed,
                    CompletedAt = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            entry = entry with
            {
                Job = entry.Job with
                {
                    Status = entry.Attempts >= _maxAttempts ? PrintStatus.Failed : PrintStatus.Queued,
                    ErrorMessage = ex.Message
                }
            };
        }

        await PersistEntryAsync(entry).ConfigureAwait(false);
        return entry.Job;
    }

    private async Task SaveJobAsync(PrintJob job, string imagePath)
    {
        var entry = new QueueEntry { Job = job, ImagePath = imagePath };
        await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    private async Task PersistEntryAsync(QueueEntry entry)
    {
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await File.WriteAllTextAsync(JobFilePath(entry.Job.Id), json).ConfigureAwait(false);
    }

    private async Task<QueueEntry?> LoadEntryAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<QueueEntry>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
    }

    private string JobFilePath(Guid id) => Path.Combine(_directory, $"{id}.json");

    private record QueueEntry
    {
        public PrintJob Job { get; init; } = new();
        public string ImagePath { get; init; } = string.Empty;
        public int Attempts { get; init; }
    }
}
