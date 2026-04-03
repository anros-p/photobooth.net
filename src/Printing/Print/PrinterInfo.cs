namespace Photobooth.Printing.Print;

public record PrinterInfo
{
    public string Name { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsOnline { get; init; }
}
