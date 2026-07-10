using Entities.Models;

namespace Services.Repositories;

public interface IAbsenceRepository
{
    Task<Absence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Absence>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Absence absence, CancellationToken cancellationToken = default);

    Task UpdateAsync(Absence absence, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}