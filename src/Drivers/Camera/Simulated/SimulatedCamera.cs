using System.Runtime.CompilerServices;
using Photobooth.Drivers.Models;
using SkiaSharp;

namespace Photobooth.Drivers.Camera.Simulated;

/// <summary>
/// A fully functional simulated camera that generates synthetic JPEG frames.
/// Used for UI development and testing without physical hardware.
/// </summary>
public sealed class SimulatedCamera : ICamera
{
    private static readonly SKColor[] FrameColors =
    [
        new SKColor(30, 30, 50),
        new SKColor(30, 50, 30),
        new SKColor(50, 30, 30),
        new SKColor(30, 40, 60),
    ];

    private readonly int _frameDelayMs;
    private bool _isConnected;
    private int _frameIndex;

    public CameraInfo Info { get; }
    public bool IsConnected => _isConnected;

    public SimulatedCamera(CameraInfo info, int frameDelayMs = 33) // ~30fps
    {
        Info = info;
        _frameDelayMs = frameDelayMs;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        EnsureConnected();

        while (!ct.IsCancellationRequested)
        {
            yield return GenerateFrame();
            await Task.Delay(_frameDelayMs, ct).ConfigureAwait(false);
        }
    }

    public Task<CapturedImage> CaptureAsync(string outputPath, CancellationToken ct = default)
    {
        EnsureConnected();

        var frame = GenerateFrame(width: 3000, height: 2000, isCaptured: true);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, frame.JpegData);

        return Task.FromResult(new CapturedImage
        {
            FilePath = outputPath,
            CapturedAt = DateTimeOffset.UtcNow
        });
    }

    public ValueTask DisposeAsync()
    {
        _isConnected = false;
        return ValueTask.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!_isConnected)
            throw new CameraException(CameraErrorCode.NotConnected, "Simulated camera is not connected.");
    }

    private CameraFrame GenerateFrame(int width = 640, int height = 480, bool isCaptured = false)
    {
        var color = FrameColors[_frameIndex % FrameColors.Length];
        if (!isCaptured)
            _frameIndex++;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(color);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };
        using var font = new SKFont(SKTypeface.Default, isCaptured ? 80 : 32);

        var label = isCaptured ? "CAPTURED" : $"SIMULATED LIVE VIEW  {DateTimeOffset.Now:HH:mm:ss.fff}";
        canvas.DrawText(label, 20, isCaptured ? height / 2f : 40, font, paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return new CameraFrame(data.ToArray(), DateTimeOffset.UtcNow);
    }
}
