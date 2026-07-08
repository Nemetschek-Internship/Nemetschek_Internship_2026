using Entities.Models;

namespace Services.Repositories;

public interface IClassScheduleEntryRepository
{
    Task<ClassScheduleEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClassScheduleEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(ClassScheduleEntry scheduleEntry, CancellationToken cancellationToken = default);

    Task UpdateAsync(ClassScheduleEntry scheduleEntry, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
