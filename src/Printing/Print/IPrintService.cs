namespace Photobooth.Printing.Print;

public interface IPrintService
{
    Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default);
    Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default);
}
