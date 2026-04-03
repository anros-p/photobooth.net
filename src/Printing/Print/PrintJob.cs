namespace Photobooth.Printing.Print;

public record PrintJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public string PrinterName { get; init; } = string.Empty;
    public string MediaSize { get; init; } = string.Empty;
    public int Copies { get; init; } = 1;
    public PrintStatus Status { get; init; } = PrintStatus.Queued;
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
}
