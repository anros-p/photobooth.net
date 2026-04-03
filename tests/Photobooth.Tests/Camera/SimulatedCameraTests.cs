using Photobooth.Drivers.Camera;
using Photobooth.Drivers.Camera.Simulated;

namespace Photobooth.Tests.Camera;

public sealed class SimulatedCameraTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly CameraInfo _info = new()
    {
        Id = "simulated-0",
        Model = "Simulated Camera",
        Port = "virtual://0",
        DriverKind = CameraDriverKind.Simulated
    };

    [Fact]
    public async Task Connect_SetsIsConnected()
    {
        await using var camera = new SimulatedCamera(_info);
        Assert.False(camera.IsConnected);

        await camera.ConnectAsync();
        Assert.True(camera.IsConnected);
    }

    [Fact]
    public async Task Disconnect_ClearsIsConnected()
    {
        await using var camera = new SimulatedCamera(_info);
        await camera.ConnectAsync();
        await camera.DisconnectAsync();

        Assert.False(camera.IsConnected);
    }

    [Fact]
    public async Task CaptureAsync_ThrowsWhenNotConnected()
    {
        await using var camera = new SimulatedCamera(_info);
        await Assert.ThrowsAsync<CameraException>(() =>
            camera.CaptureAsync(Path.Combine(_tempDir, "test.jpg")));
    }

    [Fact]
    public async Task CaptureAsync_WritesJpegFile()
    {
        await using var camera = new SimulatedCamera(_info);
        await camera.ConnectAsync();

        var outputPath = Path.Combine(_tempDir, "capture.jpg");
        var result = await camera.CaptureAsync(outputPath);

        Assert.Equal(outputPath, result.FilePath);
        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
    }

    [Fact]
    public async Task GetLiveViewStream_YieldsFrames()
    {
        await using var camera = new SimulatedCamera(_info, frameDelayMs: 10);
        await camera.ConnectAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var frames = new List<CameraFrame>();

        await foreach (var frame in camera.GetLiveViewStreamAsync(cts.Token))
        {
            frames.Add(frame);
            if (frames.Count >= 5)
            {
                cts.Cancel();
                break;
            }
        }

        Assert.Equal(5, frames.Count);
        Assert.All(frames, f => Assert.NotEmpty(f.JpegData));
    }

    [Fact]
    public async Task GetLiveViewStream_FramesHaveValidJpegHeader()
    {
        await using var camera = new SimulatedCamera(_info, frameDelayMs: 10);
        await camera.ConnectAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await foreach (var frame in camera.GetLiveViewStreamAsync(cts.Token))
        {
            // JPEG files start with FF D8
            Assert.Equal(0xFF, frame.JpegData[0]);
            Assert.Equal(0xD8, frame.JpegData[1]);
            break;
        }
    }

    [Fact]
    public async Task Discovery_ReturnsOneCamera()
    {
        var discovery = new SimulatedCameraDiscovery();
        var cameras = await discovery.DetectAsync();

        Assert.Single(cameras);
        Assert.Equal(CameraDriverKind.Simulated, cameras[0].DriverKind);
    }

    [Fact]
    public async Task Discovery_CreateCamera_ReturnsSimulatedCamera()
    {
        var discovery = new SimulatedCameraDiscovery();
        var cameras = await discovery.DetectAsync();
        var camera = discovery.CreateCamera(cameras[0]);

        Assert.IsType<SimulatedCamera>(camera);
        await camera.DisposeAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
