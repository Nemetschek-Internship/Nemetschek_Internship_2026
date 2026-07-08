using Entities.Models;

namespace Services.Interfaces.Teachers;

public interface ITeacherService
{
    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}