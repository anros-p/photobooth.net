namespace Photobooth.Drivers.Camera;

/// <summary>A single live view frame as a JPEG byte buffer.</summary>
public sealed class CameraFrame
{
    public byte[] JpegData { get; }
    public DateTimeOffset Timestamp { get; }

    public CameraFrame(byte[] jpegData, DateTimeOffset timestamp)
    {
        JpegData = jpegData;
        Timestamp = timestamp;
    }
}
