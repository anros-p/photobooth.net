using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace Photobooth.Printing.Print;

/// <summary>
/// Sends images to a printer via the Windows GDI print spooler.
/// Supports DNP and any other printer that has a Windows driver installed.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPrintService : IPrintService
{
    public Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default)
    {
        var printers = new List<PrinterInfo>();
        var defaultName = new PrinterSettings().PrinterName;

        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            var settings = new PrinterSettings { PrinterName = name };
            printers.Add(new PrinterInfo
            {
                Name = name,
                IsDefault = name == defaultName,
                IsOnline = settings.IsValid
            });
        }

        return Task.FromResult<IReadOnlyList<PrinterInfo>>(printers);
    }

    public Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        using var image = Image.FromFile(imagePath);
        using var doc = new PrintDocument();

        doc.PrinterSettings.PrinterName = options.PrinterName;
        doc.PrinterSettings.Copies = (short)Math.Clamp(options.Copies, 1, 99);

        if (!string.IsNullOrEmpty(options.MediaSize))
            ApplyMediaSize(doc, options.MediaSize);

        doc.PrintPage += (_, e) =>
        {
            if (e.Graphics is null) return;

            var bounds = e.MarginBounds;
            var srcRect = new Rectangle(0, 0, image.Width, image.Height);
            e.Graphics.DrawImage(image, bounds, srcRect, GraphicsUnit.Pixel);
        };

        doc.Print();
        return Task.CompletedTask;
    }

    private static void ApplyMediaSize(PrintDocument doc, string mediaSize)
    {
        foreach (PaperSize size in doc.PrinterSettings.PaperSizes)
        {
            if (string.Equals(size.PaperName, mediaSize, StringComparison.OrdinalIgnoreCase))
            {
                doc.DefaultPageSettings.PaperSize = size;
                return;
            }
        }
        // If the media size name isn't matched, leave the driver default in place.
    }
}
