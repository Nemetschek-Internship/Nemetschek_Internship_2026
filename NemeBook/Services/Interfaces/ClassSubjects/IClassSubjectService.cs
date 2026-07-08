using Entities.Models;

namespace Services.Interfaces.ClassSubjects;

public interface IClassSubjectService
{
    Task<ClassSubject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassSubject>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}