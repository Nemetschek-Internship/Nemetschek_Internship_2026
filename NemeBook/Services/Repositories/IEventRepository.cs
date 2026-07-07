using Entities.Models;

namespace Services.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Event schoolEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(Event schoolEvent, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
