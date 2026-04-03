namespace Photobooth.Plugins.Sharing;

public record EmailConfig
{
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Photobooth";
    public string Subject { get; init; } = "Your photos!";
    public bool UseSsl { get; init; } = true;
}
