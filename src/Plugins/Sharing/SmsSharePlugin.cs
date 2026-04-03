using System.Net.Http.Headers;
using System.Text;
using Photobooth.Drivers.Models;
using Photobooth.Plugins.Upload;

namespace Photobooth.Plugins.Sharing;

/// <summary>
/// Sends an SMS with the public image URL via a Twilio-compatible REST API.
/// The image must already be publicly accessible (e.g. uploaded by the microsite plugin).
/// <see cref="ShareJob.PublicUrl"/> is used as the image URL in the message.
/// <see cref="ShareJob.Recipient"/> is the destination phone number.
/// </summary>
public sealed class SmsSharePlugin : ISharePlugin
{
    private readonly SmsConfig _config;
    private readonly HttpClient _http;
    private readonly UploadQueue _queue;

    public string Id => "sms-share";
    public string Name => "SMS Share";
    public ShareChannel Channel => ShareChannel.Sms;

    public SmsSharePlugin(SmsConfig config, HttpClient? http = null)
    {
        _config = config;
        _http = http ?? new HttpClient();
        _queue = new UploadQueue(
            Path.Combine(Path.GetTempPath(), "photobooth", "sms_queue"),
            new SmsTransport(config, _http));
    }

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default)
        => _queue.EnqueueAsync(job, ct);

    // -----------------------------------------------------------------------

    private sealed class SmsTransport(SmsConfig config, HttpClient http) : IUploadTransport
    {
        public async Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        {
            var body = config.MessageTemplate.Replace("{url}", job.PublicUrl ?? string.Empty);

            var formData = new Dictionary<string, string>
            {
                ["To"] = job.Recipient,
                ["From"] = config.FromNumber,
                ["Body"] = body
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, config.GatewayUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            if (!string.IsNullOrEmpty(config.AccountSid))
            {
                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{config.AccountSid}:{config.AuthToken}"));
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", credentials);
            }

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return null;
        }
    }
}
