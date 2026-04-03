namespace Photobooth.Drivers.Camera;

public record CameraInfo
{
    public string Id { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Port { get; init; } = string.Empty;
    public CameraDriverKind DriverKind { get; init; }
}

public enum CameraDriverKind
{
    Simulated,
    Canon,
    Nikon,
    Gphoto2
}
