namespace Photobooth.Plugins.Sharing;

public record SmsConfig
{
    /// <summary>SMS gateway base URL (Twilio-compatible REST API).</summary>
    public string GatewayUrl { get; init; } = string.Empty;

    /// <summary>Account SID or API key sent as the HTTP Basic Auth username.</summary>
    public string AccountSid { get; init; } = string.Empty;

    /// <summary>Auth token or API secret sent as the HTTP Basic Auth password.</summary>
    public string AuthToken { get; init; } = string.Empty;

    public string FromNumber { get; init; } = string.Empty;

    /// <summary>Message body template. Use {url} as placeholder for the public image URL.</summary>
    public string MessageTemplate { get; init; } = "Here are your photos: {url}";
}
