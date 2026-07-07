using Entities.Models;

namespace Services.Repositories;

public interface IParentRepository
{
    Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Parent parent, CancellationToken cancellationToken = default);

    Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
