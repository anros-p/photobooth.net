using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Photobooth.Drivers.Camera.Canon;

/// <summary>
/// P/Invoke declarations for Canon EDSDK.
/// Requires EDSDK.dll (Windows x64) to be present in the application directory.
/// Obtain from the Canon Developer Program: https://developercommunity.usa.canon.com/
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class EdsNative
{
    private const string Dll = "EDSDK.dll";

    // ── Error codes ────────────────────────────────────────────────────────
    internal const uint EDS_ERR_OK = 0x00000000;

    // ── Camera commands ────────────────────────────────────────────────────
    internal const uint kEdsCameraCommand_TakePicture = 0x00000000;
    internal const uint kEdsCameraCommand_PressShutterButton = 0x00000004;
    internal const int kEdsCameraCommand_ShutterButton_Completely = 0x00000003;
    internal const int kEdsCameraCommand_ShutterButton_OFF = 0x00000000;

    // ── Property IDs ──────────────────────────────────────────────────────
    internal const uint kEdsPropID_Evf_OutputDevice = 0x00000500;
    internal const uint kEdsPropID_Evf_Mode = 0x00000501;
    internal const uint kEdsEvfOutputDevice_TFT = 0x00000002;

    // ── Device info ───────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    internal struct EdsDeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDeviceDescription;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szPortName;

        public uint reserved;
        public uint deviceSubType;
    }

    // ── SDK lifecycle ─────────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsInitializeSDK();

    [LibraryImport(Dll)]
    internal static partial uint EdsTerminateSDK();

    // ── Camera enumeration ────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsGetCameraList(out IntPtr outCameraListRef);

    [LibraryImport(Dll)]
    internal static partial uint EdsGetChildCount(IntPtr inRef, out int outCount);

    [LibraryImport(Dll)]
    internal static partial uint EdsGetChildAtIndex(IntPtr inRef, int inIndex, out IntPtr outRef);

    [DllImport(Dll)]
    internal static extern uint EdsGetDeviceInfo(IntPtr inCameraRef, out EdsDeviceInfo outDeviceInfo);

    [LibraryImport(Dll)]
    internal static partial uint EdsRelease(IntPtr inRef);

    // ── Session ───────────────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsOpenSession(IntPtr inCameraRef);

    [LibraryImport(Dll)]
    internal static partial uint EdsCloseSession(IntPtr inCameraRef);

    // ── Capture ───────────────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsSendCommand(IntPtr inCameraRef, uint inCommand, int inParam);

    // ── Properties ────────────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsSetPropertyData(
        IntPtr inRef, uint inPropertyID, int inParam, uint inPropertySize, ref uint inPropertyData);

    [LibraryImport(Dll)]
    internal static partial uint EdsGetPropertyData(
        IntPtr inRef, uint inPropertyID, int inParam, uint inPropertySize, out uint outPropertyData);

    // ── Live view ─────────────────────────────────────────────────────────
    [LibraryImport(Dll)]
    internal static partial uint EdsCreateMemoryStream(ulong inBufferSize, out IntPtr outStreamRef);

    [LibraryImport(Dll)]
    internal static partial uint EdsCreateEvfImageRef(IntPtr inStreamRef, out IntPtr outEvfImageRef);

    [LibraryImport(Dll)]
    internal static partial uint EdsDownloadEvfImage(IntPtr inCameraRef, IntPtr inEvfImageRef);

    [LibraryImport(Dll)]
    internal static partial uint EdsGetPointer(IntPtr inStreamRef, out IntPtr outPointer);

    [LibraryImport(Dll)]
    internal static partial uint EdsGetLength(IntPtr inStreamRef, out ulong outLength);
}
