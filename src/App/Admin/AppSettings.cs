namespace Photobooth.App.Admin;

public record AppSettings
{
    public string DataDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Photobooth");

    /// <summary>"auto" | "canon" | "nikon" | "gphoto2" | "simulated"</summary>
    public string CameraDriverOverride { get; init; } = "auto";

    // SMTP
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string SmtpFromAddress { get; init; } = string.Empty;

    // SMS gateway
    public string SmsGatewayUrl { get; init; } = string.Empty;
    public string SmsAccountSid { get; init; } = string.Empty;
    public string SmsAuthToken { get; init; } = string.Empty;
    public string SmsFromNumber { get; init; } = string.Empty;

    // Hosting / QR / Microsite
    public string HostingEndpoint { get; init; } = string.Empty;
    public string HostingApiKey { get; init; } = string.Empty;
}
