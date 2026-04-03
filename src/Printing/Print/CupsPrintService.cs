using System.Runtime.Versioning;

namespace Photobooth.Printing.Print;

/// <summary>
/// Sends images to a CUPS printer on Linux / Raspberry Pi using the <c>lp</c> command.
/// Supports DNP and any printer with a CUPS driver installed.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class CupsPrintService : IPrintService
{
    public async Task<IReadOnlyList<PrinterInfo>> GetAvailablePrintersAsync(CancellationToken ct = default)
    {
        // lpstat -a lists all accepting queues; -d shows default
        var lpstatOutput = await RunAsync("lpstat", "-a", ct).ConfigureAwait(false);
        var defaultPrinter = (await RunAsync("lpstat", "-d", ct).ConfigureAwait(false))
            .Replace("system default destination:", "").Trim();

        var printers = new List<PrinterInfo>();
        foreach (var line in lpstatOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Format: "PrinterName accepting requests since ..."
            var name = line.Split(' ')[0].Trim();
            if (string.IsNullOrEmpty(name)) continue;

            printers.Add(new PrinterInfo
            {
                Name = name,
                IsDefault = name == defaultPrinter,
                IsOnline = true
            });
        }

        return printers;
    }

    public async Task PrintAsync(string imagePath, PrintOptions options, CancellationToken ct = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        var args = BuildLpArgs(imagePath, options);
        var output = await RunAsync("lp", args, ct).ConfigureAwait(false);

        if (!output.Contains("request id"))
            throw new InvalidOperationException($"lp command did not confirm print job. Output: {output}");
    }

    private static string BuildLpArgs(string imagePath, PrintOptions options)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(options.PrinterName))
        {
            parts.Add("-d");
            parts.Add(options.PrinterName);
        }

        if (options.Copies > 1)
        {
            parts.Add("-n");
            parts.Add(options.Copies.ToString());
        }

        if (!string.IsNullOrEmpty(options.MediaSize))
        {
            parts.Add("-o");
            parts.Add($"media={options.MediaSize}");
        }

        parts.Add(imagePath);
        return string.Join(' ', parts);
    }

    private static async Task<string> RunAsync(string command, string args, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return output;
    }
}
