using System.Text.Json;
using Photobooth.Drivers.Models;

namespace Photobooth.Plugins.Upload;

/// <summary>
/// A persistent, retry-capable upload queue backed by JSON files on disk.
/// Jobs survive application restarts. Failed jobs are retried up to
/// <see cref="MaxAttempts"/> times.
/// </summary>
public sealed class UploadQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;
    private readonly IUploadTransport _transport;
    private readonly int _maxAttempts;

    public int MaxAttempts => _maxAttempts;

    public UploadQueue(string dataDirectory, IUploadTransport transport, int maxAttempts = 3)
    {
        _directory = Path.Combine(dataDirectory, "upload_queue");
        _transport = transport;
        _maxAttempts = maxAttempts;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Enqueues a new share job and immediately attempts to process it.</summary>
    public async Task<ShareJob> EnqueueAsync(ShareJob job, CancellationToken ct = default)
    {
        await SaveEntryAsync(new QueueEntry { Job = job }).ConfigureAwait(false);
        return await ProcessJobAsync(job, ct).ConfigureAwait(false);
    }

    /// <summary>Retries all queued or failed jobs (e.g. after connectivity is restored).</summary>
    public async Task RetryPendingAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var entry = await LoadEntryAsync(file, ct).ConfigureAwait(false);
            if (entry is null) continue;

            if (entry.Job.Status is ShareStatus.Queued or ShareStatus.Failed
                && entry.Attempts < _maxAttempts)
            {
                await ProcessJobAsync(entry.Job, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Returns total completed job count.</summary>
    public async Task<int> GetCompletedCountAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        var count = 0;
        foreach (var file in files)
        {
            var entry = await LoadEntryAsync(file, ct).ConfigureAwait(false);
            if (entry?.Job.Status == ShareStatus.Completed)
                count++;
        }
        return count;
    }

    // -----------------------------------------------------------------------
    // Private
    // -----------------------------------------------------------------------

    private async Task<ShareJob> ProcessJobAsync(ShareJob job, CancellationToken ct)
    {
        var entry = await LoadEntryAsync(JobFilePath(job.Id), ct).ConfigureAwait(false)
                    ?? new QueueEntry { Job = job };

        entry = entry with
        {
            Job = entry.Job with { Status = ShareStatus.Uploading },
            Attempts = entry.Attempts + 1
        };
        await SaveEntryAsync(entry).ConfigureAwait(false);

        try
        {
            var publicUrl = await _transport.ExecuteAsync(entry.Job, ct).ConfigureAwait(false);

            entry = entry with
            {
                Job = entry.Job with
                {
                    Status = ShareStatus.Completed,
                    SentAt = DateTimeOffset.UtcNow,
                    PublicUrl = publicUrl
                }
            };
        }
        catch (Exception ex)
        {
            entry = entry with
            {
                Job = entry.Job with
                {
                    Status = entry.Attempts >= _maxAttempts ? ShareStatus.Failed : ShareStatus.Queued,
                    ErrorMessage = ex.Message
                }
            };
        }

        await SaveEntryAsync(entry).ConfigureAwait(false);
        return entry.Job;
    }

    private async Task SaveEntryAsync(QueueEntry entry)
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
        public ShareJob Job { get; init; } = new();
        public int Attempts { get; init; }
    }
}
