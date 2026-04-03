using Photobooth.Drivers.Camera;
using Photobooth.Drivers.Camera.Simulated;

namespace Photobooth.Drivers.Services;

/// <summary>
/// Manages camera discovery and the active camera connection.
/// Selects the appropriate driver at runtime based on the current platform
/// and available SDK libraries.
/// </summary>
public sealed class CameraService : IAsyncDisposable
{
    private readonly IReadOnlyList<ICameraDiscovery> _discoverers;
    private ICamera? _activeCamera;

    public ICamera? ActiveCamera => _activeCamera;

    /// <param name="discoverers">
    /// Ordered list of discoverers to try. The first that returns cameras wins.
    /// If null, defaults to platform-native discoverers + simulated fallback.
    /// </param>
    public CameraService(IReadOnlyList<ICameraDiscovery>? discoverers = null)
    {
        _discoverers = discoverers ?? BuildDefaultDiscoverers();
    }

    /// <summary>
    /// Detects connected cameras across all registered discoverers.
    /// </summary>
    public async Task<IReadOnlyList<CameraInfo>> DetectCamerasAsync(CancellationToken ct = default)
    {
        var all = new List<CameraInfo>();
        foreach (var discoverer in _discoverers)
        {
            try
            {
                var found = await discoverer.DetectAsync(ct).ConfigureAwait(false);
                all.AddRange(found);
            }
            catch
            {
                // A discoverer failure (e.g., SDK not installed) should not block others.
            }
        }
        return all;
    }

    /// <summary>
    /// Connects to the specified camera and sets it as the active camera.
    /// Disconnects any previously active camera first.
    /// </summary>
    public async Task ConnectAsync(CameraInfo info, CancellationToken ct = default)
    {
        if (_activeCamera is not null)
            await DisconnectAsync(ct).ConfigureAwait(false);

        var discoverer = _discoverers.FirstOrDefault(d => d.DriverKind == info.DriverKind)
            ?? throw new InvalidOperationException($"No discoverer registered for {info.DriverKind}.");

        var camera = discoverer.CreateCamera(info);
        await camera.ConnectAsync(ct).ConfigureAwait(false);
        _activeCamera = camera;
    }

    /// <summary>
    /// Disconnects the active camera.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_activeCamera is null) return;
        await _activeCamera.DisconnectAsync(ct).ConfigureAwait(false);
        await _activeCamera.DisposeAsync().ConfigureAwait(false);
        _activeCamera = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_activeCamera is not null)
        {
            await _activeCamera.DisconnectAsync().ConfigureAwait(false);
            await _activeCamera.DisposeAsync().ConfigureAwait(false);
            _activeCamera = null;
        }
    }

    private static IReadOnlyList<ICameraDiscovery> BuildDefaultDiscoverers()
    {
        var discoverers = new List<ICameraDiscovery>();

        if (OperatingSystem.IsWindows())
        {
            // Canon and Nikon native SDKs — only registered if their DLLs exist
            // to avoid DllNotFoundException at discovery time.
            if (File.Exists("EDSDK.dll"))
                discoverers.Add(CreateWindowsDiscoverer("Photobooth.Drivers.Camera.Canon.CanonCameraDiscovery"));

            if (File.Exists("NkdPTP.dll"))
                discoverers.Add(CreateWindowsDiscoverer("Photobooth.Drivers.Camera.Nikon.NikonCameraDiscovery"));
        }
        else if (OperatingSystem.IsLinux())
        {
            discoverers.Add(CreateLinuxDiscoverer("Photobooth.Drivers.Camera.Gphoto.GphotoCameraDiscovery"));
        }

        // Simulated camera is always available as a fallback
        discoverers.Add(new SimulatedCameraDiscovery());

        return discoverers;
    }

    private static ICameraDiscovery CreateWindowsDiscoverer(string typeName)
    {
        var type = Type.GetType($"{typeName}, Photobooth.Drivers")!;
        return (ICameraDiscovery)Activator.CreateInstance(type)!;
    }

    private static ICameraDiscovery CreateLinuxDiscoverer(string typeName)
    {
        var type = Type.GetType($"{typeName}, Photobooth.Drivers")!;
        return (ICameraDiscovery)Activator.CreateInstance(type)!;
    }
}
