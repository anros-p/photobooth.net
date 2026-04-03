namespace Photobooth.Drivers.Models;

public record MicrositeBranding
{
    public string EventName { get; init; } = string.Empty;
    public string LogoPath { get; init; } = string.Empty;

    /// <summary>Primary colour as a hex string, e.g. "#FF5733".</summary>
    public string PrimaryColour { get; init; } = "#000000";

    /// <summary>Background colour as a hex string.</summary>
    public string BackgroundColour { get; init; } = "#FFFFFF";
}
