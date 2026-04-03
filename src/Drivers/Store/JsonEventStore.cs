using System.Text.Json;
using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Store;

/// <summary>
/// Persists events as individual JSON files under <see cref="DataDirectory"/>/events/.
/// Each event is stored as {id}.json so reads and writes are independent.
/// </summary>
public sealed class JsonEventStore : IEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;

    public JsonEventStore(string dataDirectory)
    {
        _directory = Path.Combine(dataDirectory, "events");
        Directory.CreateDirectory(_directory);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        var events = new List<Event>(files.Length);

        foreach (var file in files)
        {
            var evt = await ReadFileAsync(file, ct).ConfigureAwait(false);
            if (evt is not null)
                events.Add(evt);
        }

        return events.OrderBy(e => e.CreatedAt).ToList();
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        return File.Exists(path) ? await ReadFileAsync(path, ct).ConfigureAwait(false) : null;
    }

    public async Task SaveAsync(Event evt, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(evt, JsonOptions);
        await File.WriteAllTextAsync(FilePath(evt.Id), json, ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string FilePath(Guid id) => Path.Combine(_directory, $"{id}.json");

    private static async Task<Event?> ReadFileAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Event>(stream, JsonOptions, ct).ConfigureAwait(false);
    }
}
