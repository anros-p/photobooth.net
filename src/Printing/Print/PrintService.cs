namespace Photobooth.Printing.Print;

/// <summary>
/// Platform-aware factory that selects the correct <see cref="IPrintService"/>
/// implementation at runtime:
/// <list type="bullet">
///   <item>Windows → <see cref="WindowsPrintService"/> (GDI spooler)</item>
///   <item>Linux   → <see cref="CupsPrintService"/> (CUPS / lp)</item>
/// </list>
/// </summary>
public static class PrintService
{
    /// <summary>
    /// Returns the appropriate <see cref="IPrintService"/> for the current OS.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when running on an unsupported platform.
    /// </exception>
    public static IPrintService Create()
    {
        if (OperatingSystem.IsWindows())
            return CreateWindows();

        if (OperatingSystem.IsLinux())
            return CreateLinux();

        throw new PlatformNotSupportedException(
            "No print service implementation available for this platform.");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static IPrintService CreateWindows() => new WindowsPrintService();

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    private static IPrintService CreateLinux() => new CupsPrintService();
}
