using System.Text.Json;

namespace Photobooth.App.Localisation;

/// <summary>
/// JSON-backed localisation service. Language files live at
/// <c>Assets/Localisation/{langCode}.json</c> (relative to the executable).
/// Falls back to the key name when a string is missing.
/// </summary>
public sealed class LocalisationService : IStringLocalizer
{
    private readonly string _basePath;
    private Dictionary<string, string> _strings = [];

    public string CurrentLanguage { get; private set; } = "en";

    /// <summary>Raised whenever the active language changes.</summary>
    public event EventHandler? LanguageChanged;

    public LocalisationService(string? basePath = null)
    {
        _basePath = basePath
            ?? Path.Combine(AppContext.BaseDirectory, "Assets", "Localisation");

        LoadLanguage("en");
    }

    public string this[string key] =>
        _strings.TryGetValue(key, out var value) ? value : key;

    /// <summary>Switches the active language. Raises <see cref="LanguageChanged"/>.</summary>
    /// <exception cref="FileNotFoundException">When the language file does not exist.</exception>
    public void SetLanguage(string langCode)
    {
        LoadLanguage(langCode);
        CurrentLanguage = langCode;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadLanguage(string langCode)
    {
        var path = Path.Combine(_basePath, $"{langCode}.json");
        if (!File.Exists(path))
        {
            if (langCode == "en")
            {
                // No English file: start with empty dict — keys will show as-is
                _strings = [];
                return;
            }
            throw new FileNotFoundException($"Localisation file not found: {path}");
        }

        var json = File.ReadAllText(path);
        _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? [];
    }
}
