using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Gphoto;

/// <summary>
/// P/Invoke declarations for libgphoto2.
/// Requires libgphoto2.so.6 to be installed on the system (apt install libgphoto2-6).
/// </summary>
[SupportedOSPlatform("linux")]
internal static partial class GphotoNative
{
    private const string Lib = "libgphoto2.so.6";

    internal const int GP_OK = 0;

    internal const int GP_CAPTURE_IMAGE = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct CameraFilePath
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string Folder;
    }

    // ── Context ───────────────────────────────────────────────────────────
    [LibraryImport(Lib)]
    internal static partial IntPtr gp_context_new();

    [LibraryImport(Lib)]
    internal static partial void gp_context_unref(IntPtr context);

    // ── Camera ────────────────────────────────────────────────────────────
    [LibraryImport(Lib)]
    internal static partial int gp_camera_new(out IntPtr camera);

    [LibraryImport(Lib)]
    internal static partial int gp_camera_init(IntPtr camera, IntPtr context);

    [LibraryImport(Lib)]
    internal static partial int gp_camera_exit(IntPtr camera, IntPtr context);

    [LibraryImport(Lib)]
    internal static partial int gp_camera_free(IntPtr camera);

    // ── Capture ───────────────────────────────────────────────────────────
    [DllImport(Lib)]
    internal static extern int gp_camera_capture(
        IntPtr camera, int type, out CameraFilePath path, IntPtr context);

    [LibraryImport(Lib)]
    internal static partial int gp_camera_capture_preview(
        IntPtr camera, IntPtr file, IntPtr context);

    // ── File operations ───────────────────────────────────────────────────
    [LibraryImport(Lib)]
    internal static partial int gp_file_new(out IntPtr file);

    [LibraryImport(Lib)]
    internal static partial int gp_file_free(IntPtr file);

    [LibraryImport(Lib)]
    internal static partial int gp_file_get_data_and_size(
        IntPtr file, out IntPtr data, out ulong size);

    [LibraryImport(Lib)]
    internal static partial int gp_camera_file_get(
        IntPtr camera, [MarshalAs(UnmanagedType.LPStr)] string folder,
        [MarshalAs(UnmanagedType.LPStr)] string name, int type,
        IntPtr file, IntPtr context);

    [LibraryImport(Lib)]
    internal static partial int gp_file_save(
        IntPtr file, [MarshalAs(UnmanagedType.LPStr)] string filename);
}
