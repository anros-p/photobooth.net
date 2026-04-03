using Photobooth.Drivers.Models;

namespace Photobooth.Drivers.Store;

public interface IEventStore
{
    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Event evt, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
