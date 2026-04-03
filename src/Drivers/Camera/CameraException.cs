namespace Photobooth.Drivers.Camera;

public sealed class CameraException : Exception
{
    public CameraErrorCode ErrorCode { get; }

    public CameraException(CameraErrorCode code, string message) : base(message)
        => ErrorCode = code;

    public CameraException(CameraErrorCode code, string message, Exception inner) : base(message, inner)
        => ErrorCode = code;
}

public enum CameraErrorCode
{
    Unknown,
    NotConnected,
    SdkNotAvailable,
    CaptureError,
    LiveViewError,
    SessionError
}
