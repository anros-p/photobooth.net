namespace Photobooth.Drivers.Camera;

public interface ICameraDiscovery
{
    CameraDriverKind DriverKind { get; }

    /// <summary>Returns cameras currently visible to this driver.</summary>
    Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default);

    /// <summary>Creates a driver instance for the given camera.</summary>
    ICamera CreateCamera(CameraInfo info);
}
