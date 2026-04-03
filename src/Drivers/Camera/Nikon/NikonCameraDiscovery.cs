using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Nikon;

[SupportedOSPlatform("windows")]
public sealed class NikonCameraDiscovery : ICameraDiscovery
{
    public CameraDriverKind DriverKind => CameraDriverKind.Nikon;

    public Task<IReadOnlyList<CameraInfo>> DetectAsync(CancellationToken ct = default)
    {
        if (!File.Exists("NkdPTP.dll"))
            return Task.FromResult<IReadOnlyList<CameraInfo>>([]);

        var result = NikonNative.MAIDGetNumDevices(out var count);
        if (result != NikonNative.MAID_RESULT_SUCCESS || count == 0)
            return Task.FromResult<IReadOnlyList<CameraInfo>>([]);

        var cameras = new List<CameraInfo>((int)count);

        for (uint i = 0; i < count; i++)
        {
            NikonNative.MAIDOpenDevice(i, out var moduleRef);
            NikonNative.MAIDGetDeviceInfo(moduleRef, out var info);
            NikonNative.MAIDCloseDevice(moduleRef);

            cameras.Add(new CameraInfo
            {
                Id = $"nikon-{i}",
                Model = info.szDescription,
                Port = $"ptp://{i}",
                DriverKind = CameraDriverKind.Nikon
            });
        }

        return Task.FromResult<IReadOnlyList<CameraInfo>>(cameras);
    }

    public ICamera CreateCamera(CameraInfo info)
    {
        if (!uint.TryParse(info.Id.Replace("nikon-", ""), out var deviceId))
            throw new CameraException(CameraErrorCode.NotConnected,
                $"Cannot parse device ID from '{info.Id}'.");

        return new NikonCamera(info, deviceId);
    }
}
