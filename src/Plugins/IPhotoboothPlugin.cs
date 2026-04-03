using Photobooth.Drivers.Models;

namespace Photobooth.Plugins;

/// <summary>
/// Base interface for all photobooth plugins.
/// Plugins are activated per-event based on the event's plugin configuration.
/// </summary>
public interface IPhotoboothPlugin
{
    /// <summary>Stable unique identifier for this plugin (e.g. "qr-code-share").</summary>
    string Id { get; }

    /// <summary>Human-readable display name.</summary>
    string Name { get; }

    /// <summary>Called when a session is fully completed and the composed image is ready.</summary>
    Task OnSessionCompletedAsync(Session session, CancellationToken ct = default);
}
