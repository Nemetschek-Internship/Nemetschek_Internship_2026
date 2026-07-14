using Entities.ViewModels.Teachers;

namespace Services.Interfaces.Teachers;

public interface ITeacherHomeService
{
    Task<TeacherHomeViewModel?> GetHomeAsync(
        Guid userId,
        Guid? classId = null,
        bool selectDefaultClass = true,
        CancellationToken cancellationToken = default);
}
