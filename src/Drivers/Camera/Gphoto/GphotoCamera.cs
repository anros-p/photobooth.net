using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Camera.Gphoto;

/// <summary>
/// Camera driver using libgphoto2 (Linux / Raspberry Pi).
/// Supports most Canon and Nikon cameras via PTP/USB.
/// Requires libgphoto2-6 installed: sudo apt install libgphoto2-6
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class GphotoCamera : ICamera
{
    private IntPtr _camera;
    private IntPtr _context;
    private bool _isConnected;

    public CameraInfo Info { get; }
    public bool IsConnected => _isConnected;

    internal GphotoCamera(CameraInfo info)
    {
        Info = info;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _context = GphotoNative.gp_context_new();

        var result = GphotoNative.gp_camera_new(out _camera);
        if (result != GphotoNative.GP_OK)
            throw new CameraException(CameraErrorCode.SessionError,
                $"gp_camera_new failed: {result}");

        result = GphotoNative.gp_camera_init(_camera, _context);
        if (result != GphotoNative.GP_OK)
        {
            GphotoNative.gp_camera_free(_camera);
            GphotoNative.gp_context_unref(_context);
            throw new CameraException(CameraErrorCode.SessionError,
                $"gp_camera_init failed: {result}. Is a camera connected?");
        }

        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (!_isConnected) return Task.CompletedTask;

        GphotoNative.gp_camera_exit(_camera, _context);
        GphotoNative.gp_camera_free(_camera);
        GphotoNative.gp_context_unref(_context);

        _camera = IntPtr.Zero;
        _context = IntPtr.Zero;
        _isConnected = false;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CameraFrame> GetLiveViewStreamAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        EnsureConnected();

        while (!ct.IsCancellationRequested)
        {
            GphotoNative.gp_file_new(out var file);
            try
            {
                var result = GphotoNative.gp_camera_capture_preview(_camera, file, _context);
                if (result == GphotoNative.GP_OK)
                {
                    GphotoNative.gp_file_get_data_and_size(file, out var dataPtr, out var size);
                    var jpegData = new byte[size];
                    Marshal.Copy(dataPtr, jpegData, 0, (int)size);
                    yield return new CameraFrame(jpegData, DateTimeOffset.UtcNow);
                }
            }
            finally
            {
                GphotoNative.gp_file_free(file);
            }

            await Task.Delay(33, ct).ConfigureAwait(false);
        }
    }

    public Task<CapturedImage> CaptureAsync(string outputPath, CancellationToken ct = default)
    {
        EnsureConnected();

        var result = GphotoNative.gp_camera_capture(
            _camera, GphotoNative.GP_CAPTURE_IMAGE, out var filePath, _context);

        if (result != GphotoNative.GP_OK)
            throw new CameraException(CameraErrorCode.CaptureError,
                $"gp_camera_capture failed: {result}");

        GphotoNative.gp_file_new(out var file);
        try
        {
            GphotoNative.gp_camera_file_get(
                _camera, filePath.Folder, filePath.Name, 1 /* GP_FILE_TYPE_NORMAL */, file, _context);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            GphotoNative.gp_file_save(file, outputPath);
        }
        finally
        {
            GphotoNative.gp_file_free(file);
        }

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
            GphotoNative.gp_camera_exit(_camera, _context);
            GphotoNative.gp_camera_free(_camera);
            GphotoNative.gp_context_unref(_context);
        }
        return ValueTask.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!_isConnected)
            throw new CameraException(CameraErrorCode.NotConnected, "gphoto2 camera is not connected.");
    }
}
