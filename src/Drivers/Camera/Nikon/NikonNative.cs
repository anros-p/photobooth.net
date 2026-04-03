using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Nikon;

/// <summary>
/// P/Invoke declarations for the Nikon SDK (Type0003/Type0004).
/// Requires NkdPTP.dll (Windows x64) to be present in the application directory.
/// Obtain through the Nikon Developer Program.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class NikonNative
{
    private const string Dll = "NkdPTP.dll";

    internal const int MAID_RESULT_SUCCESS = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct NkMAIDDeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szVendorName;
    }

    [LibraryImport(Dll, EntryPoint = "MAIDGetNumDevices")]
    internal static partial int MAIDGetNumDevices(out uint count);

    [LibraryImport(Dll, EntryPoint = "MAIDOpenDevice")]
    internal static partial int MAIDOpenDevice(uint deviceId, out IntPtr moduleRef);

    [LibraryImport(Dll, EntryPoint = "MAIDCloseDevice")]
    internal static partial int MAIDCloseDevice(IntPtr moduleRef);

    [DllImport(Dll, EntryPoint = "MAIDGetDeviceInfo")]
    internal static extern int MAIDGetDeviceInfo(IntPtr moduleRef, out NkMAIDDeviceInfo deviceInfo);

    [LibraryImport(Dll, EntryPoint = "MAIDCapture")]
    internal static partial int MAIDCapture(IntPtr moduleRef);

    [LibraryImport(Dll, EntryPoint = "MAIDStartLiveView")]
    internal static partial int MAIDStartLiveView(IntPtr moduleRef);

    [LibraryImport(Dll, EntryPoint = "MAIDStopLiveView")]
    internal static partial int MAIDStopLiveView(IntPtr moduleRef);

    [LibraryImport(Dll, EntryPoint = "MAIDGetLiveViewImage")]
    internal static partial int MAIDGetLiveViewImage(IntPtr moduleRef, IntPtr buffer, ref uint bufferSize);
}
