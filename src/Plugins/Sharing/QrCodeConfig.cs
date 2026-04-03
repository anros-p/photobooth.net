namespace Photobooth.Plugins.Sharing;

public record QrCodeConfig
{
    /// <summary>HTTP endpoint that accepts a multipart/form-data file upload and returns a JSON body with a "url" field.</summary>
    public string UploadEndpoint { get; init; } = string.Empty;

    /// <summary>API key sent in the <c>X-Api-Key</c> request header.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Directory where generated QR code PNG files are saved.</summary>
    public string QrOutputDirectory { get; init; } = Path.GetTempPath();

    /// <summary>Pixel size of each QR module (higher = larger image).</summary>
    public int PixelsPerModule { get; init; } = 10;
}
