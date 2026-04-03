using System.Text.Json;
using Photobooth.Drivers.Models;
using Photobooth.Plugins.Upload;

namespace Photobooth.Plugins.Sharing;

/// <summary>
/// Uploads the composed image to the branded microsite endpoint, tagged with
/// the event ID and session ID. The returned public URL is stored on the share job.
/// </summary>
public sealed class MicrositeSharePlugin : ISharePlugin
{
    private readonly MicrositeConfig _config;
    private readonly HttpClient _http;
    private readonly UploadQueue _queue;

    public string Id => "microsite-share";
    public string Name => "Microsite Share";
    public ShareChannel Channel => ShareChannel.Microsite;

    public MicrositeSharePlugin(MicrositeConfig config, HttpClient? http = null)
    {
        _config = config;
        _http = http ?? new HttpClient();
        _queue = new UploadQueue(
            Path.Combine(Path.GetTempPath(), "photobooth", "microsite_queue"),
            new MicrositeTransport(config, _http));
    }

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default)
        => _queue.EnqueueAsync(job, ct);

    // -----------------------------------------------------------------------

    private sealed class MicrositeTransport(MicrositeConfig config, HttpClient http) : IUploadTransport
    {
        public async Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        {
            await using var stream = File.OpenRead(job.FilePath);
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);

            content.Add(fileContent, "file", Path.GetFileName(job.FilePath));
            content.Add(new StringContent(job.SessionId.ToString()), "sessionId");
            content.Add(new StringContent(job.Id.ToString()), "jobId");

            using var request = new HttpRequestMessage(HttpMethod.Post, config.UploadEndpoint)
            {
                Content = content
            };
            request.Headers.Add("X-Api-Key", config.ApiKey);

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("url").GetString()
                   ?? throw new InvalidOperationException("Microsite response missing 'url' field.");
        }
    }
}
