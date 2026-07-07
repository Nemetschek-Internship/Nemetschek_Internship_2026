using Entities.Models;

namespace Services.Repositories;

public interface IClassSubjectRepository
{
    Task<ClassSubject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassSubject>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default);

    Task UpdateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
