using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Photobooth.Drivers.Models;
using Photobooth.Plugins.Upload;

namespace Photobooth.Plugins.Sharing;

/// <summary>
/// Sends the composed image as an email attachment via SMTP (MailKit).
/// <see cref="ShareJob.Recipient"/> is used as the To address.
/// </summary>
public sealed class EmailSharePlugin : ISharePlugin
{
    private readonly EmailConfig _config;
    private readonly UploadQueue _queue;

    public string Id => "email-share";
    public string Name => "Email Share";
    public ShareChannel Channel => ShareChannel.Email;

    public EmailSharePlugin(EmailConfig config)
    {
        _config = config;
        _queue = new UploadQueue(
            Path.Combine(Path.GetTempPath(), "photobooth", "email_queue"),
            new EmailTransport(config));
    }

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default)
        => _queue.EnqueueAsync(job, ct);

    // -----------------------------------------------------------------------

    private sealed class EmailTransport(EmailConfig config) : IUploadTransport
    {
        public async Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
            message.To.Add(MailboxAddress.Parse(job.Recipient));
            message.Subject = config.Subject;

            var builder = new BodyBuilder
            {
                TextBody = "Thank you for using our photobooth! Your photo is attached."
            };
            await builder.Attachments.AddAsync(job.FilePath, ct).ConfigureAwait(false);
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var secureOptions = config.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, secureOptions, ct)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(config.Username))
                await client.AuthenticateAsync(config.Username, config.Password, ct)
                    .ConfigureAwait(false);

            await client.SendAsync(message, ct).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, ct).ConfigureAwait(false);

            return null; // Email delivery has no public URL
        }
    }
}
