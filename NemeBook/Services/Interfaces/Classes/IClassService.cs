using Entities.Models;

namespace Services.Interfaces.Classes;

public interface IClassService
{
    Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Class classEntity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Class classEntity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}