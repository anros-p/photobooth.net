using System.Text.Json;
using Photobooth.Drivers.Models;
using Photobooth.Plugins.Upload;
using QRCoder;

namespace Photobooth.Plugins.Sharing;

/// <summary>
/// Uploads the composed image to the configured endpoint, receives a public URL,
/// and generates a QR code PNG pointing to that URL.
/// The QR code PNG is saved to <see cref="QrCodeConfig.QrOutputDirectory"/> and the path
/// is stored in <see cref="ShareJob.PublicUrl"/> (prefixed with <c>file://</c>).
/// </summary>
public sealed class QrCodeSharePlugin : ISharePlugin
{
    private readonly QrCodeConfig _config;
    private readonly HttpClient _http;
    private readonly UploadQueue _queue;

    public string Id => "qr-code-share";
    public string Name => "QR Code Share";
    public ShareChannel Channel => ShareChannel.QrCode;

    public QrCodeSharePlugin(QrCodeConfig config, HttpClient? http = null)
    {
        _config = config;
        _http = http ?? new HttpClient();
        _queue = new UploadQueue(
            Path.Combine(Path.GetTempPath(), "photobooth", "qr_upload"),
            new QrUploadTransport(_config, _http));
    }

    public Task OnSessionCompletedAsync(Session session, CancellationToken ct = default)
        => Task.CompletedTask; // Sharing is triggered explicitly via ShareAsync

    public Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default)
        => _queue.EnqueueAsync(job, ct);

    // -----------------------------------------------------------------------

    private sealed class QrUploadTransport(QrCodeConfig config, HttpClient http) : IUploadTransport
    {
        public async Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default)
        {
            // 1. Upload image
            await using var stream = File.OpenRead(job.FilePath);
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            content.Add(fileContent, "file", Path.GetFileName(job.FilePath));

            using var request = new HttpRequestMessage(HttpMethod.Post, config.UploadEndpoint)
            {
                Content = content
            };
            request.Headers.Add("X-Api-Key", config.ApiKey);

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var imageUrl = doc.RootElement.GetProperty("url").GetString()
                           ?? throw new InvalidOperationException("Upload response missing 'url' field.");

            // 2. Generate QR code PNG
            Directory.CreateDirectory(config.QrOutputDirectory);
            var qrPath = Path.Combine(config.QrOutputDirectory, $"{job.Id}.png");

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(imageUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(config.PixelsPerModule);
            await File.WriteAllBytesAsync(qrPath, pngBytes, ct).ConfigureAwait(false);

            return qrPath;
        }
    }
}
