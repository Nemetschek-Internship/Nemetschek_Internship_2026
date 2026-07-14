using Entities.Models;

namespace Services.Interfaces.Parents;

public interface IParentService
{
    Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Parent parent, CancellationToken cancellationToken = default);
    Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}