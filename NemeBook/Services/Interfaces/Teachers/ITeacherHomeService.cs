using Entities.ViewModels.Teachers;

namespace Services.Interfaces.Teachers;

public interface ITeacherHomeService
{
    Task<TeacherHomeViewModel?> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default);
}
