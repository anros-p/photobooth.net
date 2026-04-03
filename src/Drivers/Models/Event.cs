namespace Photobooth.Drivers.Models;

public record Event
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // --- Capture ---
    public CaptureMode CaptureMode { get; init; } = CaptureMode.Still;

    /// <summary>Countdown duration in seconds before each shot.</summary>
    public int CountdownSeconds { get; init; } = 3;

    /// <summary>For GIF/Boomerang: number of frames to capture.</summary>
    public int GifFrameCount { get; init; } = 10;

    /// <summary>For GIF/Boomerang: target frames per second.</summary>
    public int GifFrameRate { get; init; } = 10;

    // --- Layout ---
    public LayoutTemplate? Layout { get; init; }

    // --- Screensaver ---
    /// <summary>Path to video or image file to display when idle.</summary>
    public string ScreensaverMediaPath { get; init; } = string.Empty;

    /// <summary>Idle time in seconds before screensaver activates.</summary>
    public int ScreensaverIdleSeconds { get; init; } = 60;

    // --- Overlays ---
    public IReadOnlyList<OverlayAsset> OverlayAssets { get; init; } = [];

    // --- Printing ---
    public bool PrintingEnabled { get; init; }
    public int MaxPrints { get; init; }

    // --- Sharing ---
    public ShareChannel EnabledShareChannels { get; init; } = ShareChannel.None;

    // --- Gallery / Microsite ---
    public bool GalleryAccessEnabled { get; init; }
    public string GalleryAccessCode { get; init; } = string.Empty;
    public MicrositeBranding? MicrositeBranding { get; init; }

    // --- Localisation ---
    /// <summary>BCP-47 language tag for the kiosk default, e.g. "en", "fr".</summary>
    public string DefaultLanguage { get; init; } = "en";
    public IReadOnlyList<string> AvailableLanguages { get; init; } = ["en"];

    // --- Plugin configuration ---
    /// <summary>Per-plugin configuration, keyed by plugin ID.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PluginConfig { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, string>>();
}
