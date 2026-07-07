using Entities.Models;

namespace Services.Repositories;

public interface IClassRepository
{
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Class schoolClass, CancellationToken cancellationToken = default);

    Task UpdateAsync(Class schoolClass, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
