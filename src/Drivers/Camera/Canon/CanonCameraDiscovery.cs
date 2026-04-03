using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Canon;

/// <summary>
/// Detects Canon EOS cameras connected via USB using EDSDK (Windows only).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CanonCameraDiscovery : ICameraDiscovery
{
    private bool _sdkInitialised;

    public CameraDriverKind DriverKind => CameraDriverKind.Canon;

    public Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default)
    {
        if (!File.Exists("EDSDK.dll"))
            return Task.FromResult<IReadOnlyList<CameraInfo>>([]);

        EnsureSdkInitialised();

        var err = EdsNative.EdsGetCameraList(out var listRef);
        if (err != EdsNative.EDS_ERR_OK)
            return Task.FromResult<IReadOnlyList<CameraInfo>>([]);

        try
        {
            EdsNative.EdsGetChildCount(listRef, out var count);
            var cameras = new List<CameraInfo>(count);

            for (var i = 0; i < count; i++)
            {
                EdsNative.EdsGetChildAtIndex(listRef, i, out var cameraRef);
                EdsNative.EdsGetDeviceInfo(cameraRef, out var info);

                cameras.Add(new CameraInfo
                {
                    Id = $"canon-{i}",
                    Model = info.szDeviceDescription,
                    Port = info.szPortName,
                    DriverKind = CameraDriverKind.Canon
                });

                // Keep cameraRef alive — passed to CanonCamera
            }

            return Task.FromResult<IReadOnlyList<CameraInfo>>(cameras);
        }
        finally
        {
            EdsNative.EdsRelease(listRef);
        }
    }

    public ICamera CreateCamera(CameraInfo info)
    {
        EnsureSdkInitialised();

        // Re-detect to get a fresh cameraRef for this camera
        EdsNative.EdsGetCameraList(out var listRef);
        EdsNative.EdsGetChildCount(listRef, out var count);

        for (var i = 0; i < count; i++)
        {
            EdsNative.EdsGetChildAtIndex(listRef, i, out var cameraRef);
            EdsNative.EdsGetDeviceInfo(cameraRef, out var deviceInfo);

            if (deviceInfo.szPortName == info.Port)
            {
                EdsNative.EdsRelease(listRef);
                return new CanonCamera(info, cameraRef);
            }

            EdsNative.EdsRelease(cameraRef);
        }

        EdsNative.EdsRelease(listRef);
        throw new CameraException(CameraErrorCode.NotConnected,
            $"Canon camera on port '{info.Port}' is no longer connected.");
    }

    private void EnsureSdkInitialised()
    {
        if (_sdkInitialised) return;
        var err = EdsNative.EdsInitializeSDK();
        if (err != EdsNative.EDS_ERR_OK)
            throw new CameraException(CameraErrorCode.SdkNotAvailable,
                $"EdsInitializeSDK failed: 0x{err:X8}");
        _sdkInitialised = true;
    }
}
