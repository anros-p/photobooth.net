using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Camera;

public interface ICamera : IAsyncDisposable
{
    CameraInfo Info { get; }
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Streams live view JPEG frames continuously until the token is cancelled.
    /// Typical frame rate is ~30fps depending on camera and USB bandwidth.
    /// </summary>
    IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(CancellationToken ct);

    /// <summary>
    /// Triggers the shutter and downloads the captured image to <paramref name="outputPath"/>.
    /// Returns a <see cref="CapturedImage"/> referencing the saved file.
    /// </summary>
    Task<CapturedImage> CaptureAsync(string outputPath, CancellationToken ct = default);
}
