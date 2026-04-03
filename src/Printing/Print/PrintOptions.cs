namespace Photobooth.Printing.Print;

public record PrintOptions
{
    public string PrinterName { get; init; } = string.Empty;

    /// <summary>Media size name as understood by the OS driver, e.g. "4x6", "5x7".</summary>
    public string MediaSize { get; init; } = string.Empty;

    public int Copies { get; init; } = 1;
}
