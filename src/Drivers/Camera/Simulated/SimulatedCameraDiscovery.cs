namespace Photobooth.Drivers.Camera.Simulated;

public sealed class SimulatedCameraDiscovery : ICameraDiscovery
{
    public CameraDriverKind DriverKind => CameraDriverKind.Simulated;

    public Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default)
    {
        IReadOnlyList<CameraInfo> cameras =
        [
            new CameraInfo
            {
                Id = "simulated-0",
                Model = "Simulated Camera",
                Port = "virtual://0",
                DriverKind = CameraDriverKind.Simulated
            }
        ];

        return Task.FromResult(cameras);
    }

    public ICamera CreateCamera(CameraInfo info) => new SimulatedCamera(info);
}
