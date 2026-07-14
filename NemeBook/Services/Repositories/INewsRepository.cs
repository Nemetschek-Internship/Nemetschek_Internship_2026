using Entities.Models;

namespace Services.Repositories;

public interface INewsRepository
{
    Task<News?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<News>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(News news, CancellationToken cancellationToken = default);

    Task UpdateAsync(News news, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
