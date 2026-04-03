using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Camera.Nikon;

[SupportedOSPlatform("windows")]
public sealed class NikonCamera : ICamera
{
    private readonly uint _deviceId;
    private IntPtr _moduleRef;
    private bool _isConnected;
    private bool _liveViewActive;

    public CameraInfo Info { get; }
    public bool IsConnected => _isConnected;

    internal NikonCamera(CameraInfo info, uint deviceId)
    {
        Info = info;
        _deviceId = deviceId;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        ThrowIfSdkMissing();

        var result = NikonNative.MAIDOpenDevice(_deviceId, out _moduleRef);
        if (result != NikonNative.MAID_RESULT_SUCCESS)
            throw new CameraException(CameraErrorCode.SessionError,
                $"MAIDOpenDevice failed: {result}");

        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (!_isConnected) return Task.CompletedTask;

        if (_liveViewActive)
        {
            NikonNative.MAIDStopLiveView(_moduleRef);
            _liveViewActive = false;
        }

        NikonNative.MAIDCloseDevice(_moduleRef);
        _moduleRef = IntPtr.Zero;
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        EnsureConnected();

        NikonNative.MAIDStartLiveView(_moduleRef);
        _liveViewActive = true;

        try
        {
            const uint bufferSize = 2 * 1024 * 1024;
            var buffer = Marshal.AllocHGlobal((int)bufferSize);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var size = bufferSize;
                    var result = NikonNative.MAIDGetLiveViewImage(_moduleRef, buffer, ref size);

                    if (result == NikonNative.MAID_RESULT_SUCCESS && size > 0)
                    {
                        var jpegData = new byte[size];
                        Marshal.Copy(buffer, jpegData, 0, (int)size);
                        yield return new CameraFrame(jpegData, DateTimeOffset.UtcNow);
                    }

                    await Task.Delay(33, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            NikonNative.MAIDStopLiveView(_moduleRef);
            _liveViewActive = false;
        }
    }

    public Task<CapturedImage> CaptureAsync(string outputPath, CancellationToken ct = default)
    {
        EnsureConnected();

        var result = NikonNative.MAIDCapture(_moduleRef);
        if (result != NikonNative.MAID_RESULT_SUCCESS)
            throw new CameraException(CameraErrorCode.CaptureError,
                $"MAIDCapture failed: {result}");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        return Task.FromResult(new CapturedImage
        {
            FilePath = outputPath,
            CapturedAt = DateTimeOffset.UtcNow
        });
    }

    public ValueTask DisposeAsync()
    {
        if (_isConnected)
        {
            if (_liveViewActive) NikonNative.MAIDStopLiveView(_moduleRef);
            NikonNative.MAIDCloseDevice(_moduleRef);
        }
        return ValueTask.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!_isConnected)
            throw new CameraException(CameraErrorCode.NotConnected, "Nikon camera is not connected.");
    }

    private static void ThrowIfSdkMissing()
    {
        if (!File.Exists("NkdPTP.dll"))
            throw new CameraException(CameraErrorCode.SdkNotAvailable,
                "NkdPTP.dll not found. Place Nikon SDK binaries in the application directory.");
    }
}
