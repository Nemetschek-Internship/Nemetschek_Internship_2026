using Entities.ViewModels.Students;

namespace Services.Interfaces.Students;

public interface IStudentHomeService
{
    Task<StudentHomeViewModel?> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<StudentCalendarViewModel?> GetCalendarAsync(
        Guid userId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);
}
