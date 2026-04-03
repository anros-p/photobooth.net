using Photobooth.Drivers.Models;

namespace Photobooth.Plugins.Upload;

/// <summary>
/// Executes the actual delivery for an upload job.
/// Implementations cover HTTP upload, email sending, SMS sending, etc.
/// </summary>
public interface IUploadTransport
{
    /// <summary>
    /// Performs the delivery.
    /// </summary>
    /// <returns>An optional public URL for the delivered content, or <c>null</c> if not applicable.</returns>
    Task<string?> ExecuteAsync(ShareJob job, CancellationToken ct = default);
}
