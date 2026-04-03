using Photobooth.Drivers.Camera;
using Photobooth.Drivers.Camera.Gif;
using Photobooth.Drivers.Camera.Simulated;
using Photobooth.Drivers.Models;

namespace Photobooth.Tests.Camera;

public sealed class GifCaptureSessionTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Fact]
    public async Task CaptureAsync_ProducesGifFile()
    {
        await using var camera = await MakeCamera();
        var session = new GifCaptureSession(camera, frameCount: 3, frameDelayMs: 50, CaptureMode.Gif);

        var outputPath = Path.Combine(_tempDir, "output.gif");
        var result = await session.CaptureAsync(Path.Combine(_tempDir, "frames"), outputPath);

        Assert.True(File.Exists(outputPath));
        Assert.Equal(outputPath, result.FilePath);

        // GIF files start with GIF87a or GIF89a
        var header = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(outputPath)[..6]);
        Assert.StartsWith("GIF", header);
    }

    [Fact]
    public async Task CaptureAsync_BoomerangProducesGifFile()
    {
        await using var camera = await MakeCamera();
        var session = new GifCaptureSession(camera, frameCount: 4, frameDelayMs: 50, CaptureMode.Boomerang);

        var outputPath = Path.Combine(_tempDir, "boomerang.gif");
        await session.CaptureAsync(Path.Combine(_tempDir, "frames_b"), outputPath);

        Assert.True(File.Exists(outputPath));
        var header = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(outputPath)[..6]);
        Assert.StartsWith("GIF", header);
    }

    [Fact]
    public async Task Constructor_ThrowsForStillMode()
    {
        await using var camera = await MakeCamera();
        Assert.Throws<ArgumentException>(() =>
            new GifCaptureSession(camera, 3, 100, CaptureMode.Still));
    }

    [Fact]
    public async Task CaptureAsync_CancellationStopsCapture()
    {
        await using var camera = await MakeCamera();
        var session = new GifCaptureSession(camera, frameCount: 20, frameDelayMs: 200, CaptureMode.Gif);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var outputPath = Path.Combine(_tempDir, "cancelled.gif");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.CaptureAsync(Path.Combine(_tempDir, "frames_c"), outputPath, cts.Token));
    }

    private static async Task<SimulatedCamera> MakeCamera()
    {
        var camera = new SimulatedCamera(new CameraInfo
        {
            Id = "simulated-0", Model = "Simulated Camera",
            Port = "virtual://0", DriverKind = CameraDriverKind.Simulated
        });
        await camera.ConnectAsync();
        return camera;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
