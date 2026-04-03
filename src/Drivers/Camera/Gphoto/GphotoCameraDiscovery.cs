using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Gphoto;

[SupportedOSPlatform("linux")]
public sealed class GphotoCameraDiscovery : ICameraDiscovery
{
    public CameraDriverKind DriverKind => CameraDriverKind.Gphoto2;

    public Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default)
    {
        // libgphoto2 detects the first available camera during gp_camera_init.
        // For a full multi-camera implementation, gp_camera_autodetect() would be used.
        // For now we report a single virtual entry; the driver confirms detection on connect.
        IReadOnlyList<CameraInfo> cameras =
        [
            new CameraInfo
            {
                Id = "gphoto2-auto",
                Model = "USB Camera (gphoto2)",
                Port = "usb:",
                DriverKind = CameraDriverKind.Gphoto2
            }
        ];

        return Task.FromResult(cameras);
    }

    public ICamera CreateCamera(CameraInfo info) => new GphotoCamera(info);
}
