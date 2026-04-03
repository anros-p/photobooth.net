using Photobooth.Drivers.Camera;
using Photobooth.Drivers.Camera.Simulated;
using Photobooth.Drivers.Services;

namespace Photobooth.Tests.Camera;

public sealed class CameraServiceTests
{
    [Fact]
    public async Task DetectCameras_IncludesSimulated()
    {
        var service = new CameraService([new SimulatedCameraDiscovery()]);
        var cameras = await service.DetectCamerasAsync();

        Assert.NotEmpty(cameras);
        Assert.Contains(cameras, c => c.DriverKind == CameraDriverKind.Simulated);
    }

    [Fact]
    public async Task ConnectAsync_SetsActiveCamera()
    {
        await using var service = new CameraService([new SimulatedCameraDiscovery()]);
        var cameras = await service.DetectCamerasAsync();

        await service.ConnectAsync(cameras[0]);

        Assert.NotNull(service.ActiveCamera);
        Assert.True(service.ActiveCamera!.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_ClearsActiveCamera()
    {
        await using var service = new CameraService([new SimulatedCameraDiscovery()]);
        var cameras = await service.DetectCamerasAsync();
        await service.ConnectAsync(cameras[0]);

        await service.DisconnectAsync();

        Assert.Null(service.ActiveCamera);
    }

    [Fact]
    public async Task ConnectAsync_DisconnectsPreviousCamera()
    {
        await using var service = new CameraService([new SimulatedCameraDiscovery()]);
        var cameras = await service.DetectCamerasAsync();

        await service.ConnectAsync(cameras[0]);
        var firstCamera = service.ActiveCamera!;

        await service.ConnectAsync(cameras[0]);

        // First camera should have been disconnected; active camera is now the new one
        Assert.False(firstCamera.IsConnected);
        Assert.NotNull(service.ActiveCamera);
    }

    [Fact]
    public async Task DisposeAsync_DisconnectsActiveCamera()
    {
        var service = new CameraService([new SimulatedCameraDiscovery()]);
        var cameras = await service.DetectCamerasAsync();
        await service.ConnectAsync(cameras[0]);
        var camera = service.ActiveCamera!;

        await service.DisposeAsync();

        Assert.False(camera.IsConnected);
    }
}
