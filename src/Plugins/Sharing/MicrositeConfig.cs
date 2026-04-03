namespace Photobooth.Plugins.Sharing;

public record MicrositeConfig
{
    /// <summary>HTTP endpoint that accepts a multipart/form-data file upload tagged with event metadata.</summary>
    public string UploadEndpoint { get; init; } = string.Empty;

    /// <summary>API key sent in the <c>X-Api-Key</c> request header.</summary>
    public string ApiKey { get; init; } = string.Empty;
}
