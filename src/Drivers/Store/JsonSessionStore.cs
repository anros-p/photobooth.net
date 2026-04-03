using System.Text.Json;
using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Store;

/// <summary>
/// Persists sessions as individual JSON files under <see cref="dataDirectory"/>/sessions/.
/// File names are {sessionId}.json.
/// </summary>
public sealed class JsonSessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _directory;

    public JsonSessionStore(string dataDirectory)
    {
        _directory = Path.Combine(dataDirectory, "sessions");
        Directory.CreateDirectory(_directory);
    }

    public async Task<IReadOnlyList<Session>> GetByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        var sessions = new List<Session>();

        foreach (var file in files)
        {
            var session = await ReadFileAsync(file, ct).ConfigureAwait(false);
            if (session?.EventId == eventId)
                sessions.Add(session);
        }

        return sessions.OrderBy(s => s.StartedAt).ToList();
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        return File.Exists(path) ? await ReadFileAsync(path, ct).ConfigureAwait(false) : null;
    }

    public async Task SaveAsync(Session session, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(FilePath(session.Id), json, ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string FilePath(Guid id) => Path.Combine(_directory, $"{id}.json");

    private static async Task<Session?> ReadFileAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Session>(stream, JsonOptions, ct).ConfigureAwait(false);
    }
}
