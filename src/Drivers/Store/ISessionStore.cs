using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Store;

public interface ISessionStore
{
    Task<IReadOnlyList<Session>> GetByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Session session, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
