using Photobooth.Drivers.Models;

namespace Photobooth.Plugins;

/// <summary>
/// A plugin that shares the composed image via a specific channel
/// (QR code, email, SMS, or microsite).
/// </summary>
public interface ISharePlugin : IPhotoboothPlugin
{
    /// <summary>The share channel this plugin handles.</summary>
    ShareChannel Channel { get; }

    /// <summary>
    /// Performs the share operation for a queued <see cref="ShareJob"/>.
    /// Returns the updated job (with <see cref="ShareStatus.Completed"/> or error info).
    /// </summary>
    Task<ShareJob> ShareAsync(ShareJob job, CancellationToken ct = default);
}
