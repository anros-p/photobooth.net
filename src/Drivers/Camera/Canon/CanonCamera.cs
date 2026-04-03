using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Camera.Canon;

/// <summary>
/// Canon EOS camera driver using EDSDK via P/Invoke (Windows only).
/// Requires EDSDK.dll in the application directory.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CanonCamera : ICamera
{
    private IntPtr _cameraRef;
    private bool _isConnected;
    private bool _liveViewActive;

    public CameraInfo Info { get; }
    public bool IsConnected => _isConnected;

    internal CanonCamera(CameraInfo info, IntPtr cameraRef)
    {
        Info = info;
        _cameraRef = cameraRef;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        ThrowIfSdkMissing();

        var err = EdsNative.EdsOpenSession(_cameraRef);
        if (err != EdsNative.EDS_ERR_OK)
            throw new CameraException(CameraErrorCode.SessionError, $"EdsOpenSession failed: 0x{err:X8}");

        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (!_isConnected) return Task.CompletedTask;

        if (_liveViewActive)
            StopLiveView();

        EdsNative.EdsCloseSession(_cameraRef);
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        EnsureConnected();
        StartLiveView();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var frame = DownloadLiveViewFrame();
                if (frame is not null)
                    yield return frame;

                await Task.Delay(33, ct).ConfigureAwait(false); // ~30fps
            }
        }
        finally
        {
            StopLiveView();
        }
    }

    public Task<CapturedImage> CaptureAsync(string outputPath, CancellationToken ct = default)
    {
        EnsureConnected();

        // Press shutter button fully
        var err = EdsNative.EdsSendCommand(
            _cameraRef,
            EdsNative.kEdsCameraCommand_PressShutterButton,
            EdsNative.kEdsCameraCommand_ShutterButton_Completely);

        if (err != EdsNative.EDS_ERR_OK)
            throw new CameraException(CameraErrorCode.CaptureError, $"Shutter command failed: 0x{err:X8}");

        // Release shutter button
        EdsNative.EdsSendCommand(
            _cameraRef,
            EdsNative.kEdsCameraCommand_PressShutterButton,
            EdsNative.kEdsCameraCommand_ShutterButton_OFF);

        // NOTE: In a full implementation, we subscribe to the EdsObjectEvent_DirItemCreated
        // event callback to receive the downloaded file path from the camera.
        // For now we return a placeholder — the event-based download is wired in CameraService.
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
            if (_liveViewActive) StopLiveView();
            EdsNative.EdsCloseSession(_cameraRef);
        }

        if (_cameraRef != IntPtr.Zero)
        {
            EdsNative.EdsRelease(_cameraRef);
            _cameraRef = IntPtr.Zero;
        }

        return ValueTask.CompletedTask;
    }

    private void StartLiveView()
    {
        uint evfMode = 1;
        EdsNative.EdsSetPropertyData(_cameraRef, EdsNative.kEdsPropID_Evf_Mode, 0, sizeof(uint), ref evfMode);

        uint outputDevice = EdsNative.kEdsEvfOutputDevice_TFT;
        EdsNative.EdsSetPropertyData(_cameraRef, EdsNative.kEdsPropID_Evf_OutputDevice, 0, sizeof(uint), ref outputDevice);

        _liveViewActive = true;
    }

    private void StopLiveView()
    {
        uint outputDevice = 0;
        EdsNative.EdsSetPropertyData(_cameraRef, EdsNative.kEdsPropID_Evf_OutputDevice, 0, sizeof(uint), ref outputDevice);

        uint evfMode = 0;
        EdsNative.EdsSetPropertyData(_cameraRef, EdsNative.kEdsPropID_Evf_Mode, 0, sizeof(uint), ref evfMode);

        _liveViewActive = false;
    }

    private CameraFrame? DownloadLiveViewFrame()
    {
        const ulong bufferSize = 2 * 1024 * 1024; // 2MB

        var err = EdsNative.EdsCreateMemoryStream(bufferSize, out var streamRef);
        if (err != EdsNative.EDS_ERR_OK) return null;

        try
        {
            err = EdsNative.EdsCreateEvfImageRef(streamRef, out var evfImageRef);
            if (err != EdsNative.EDS_ERR_OK) return null;

            try
            {
                err = EdsNative.EdsDownloadEvfImage(_cameraRef, evfImageRef);
                if (err != EdsNative.EDS_ERR_OK) return null;

                EdsNative.EdsGetLength(streamRef, out var length);
                EdsNative.EdsGetPointer(streamRef, out var pointer);

                var jpegData = new byte[length];
                Marshal.Copy(pointer, jpegData, 0, (int)length);
                return new CameraFrame(jpegData, DateTimeOffset.UtcNow);
            }
            finally
            {
                EdsNative.EdsRelease(evfImageRef);
            }
        }
        finally
        {
            EdsNative.EdsRelease(streamRef);
        }
    }

    private void EnsureConnected()
    {
        if (!_isConnected)
            throw new CameraException(CameraErrorCode.NotConnected, "Canon camera is not connected.");
    }

    private static void ThrowIfSdkMissing()
    {
        if (!File.Exists("EDSDK.dll"))
            throw new CameraException(CameraErrorCode.SdkNotAvailable,
                "EDSDK.dll not found. Place Canon EDSDK binaries in the application directory.");
    }
}
