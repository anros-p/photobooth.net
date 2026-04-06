using System.Text.Json;

namespace Photobooth.App.Admin;

/// <summary>Persists <see cref="AppSettings"/> to a single JSON file.</summary>
public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public AppSettingsStore(string configDirectory)
    {
        _filePath = Path.Combine(configDirectory, "app_settings.json");
        Directory.CreateDirectory(configDirectory);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct)
                   .ConfigureAwait(false)
               ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct).ConfigureAwait(false);
    }
}
