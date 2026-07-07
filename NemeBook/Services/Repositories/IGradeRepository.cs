using Entities.Models;

namespace Services.Repositories;

public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Grade grade, CancellationToken cancellationToken = default);

    Task UpdateAsync(Grade grade, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
