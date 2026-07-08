using Entities.ViewModels.Grades;

namespace Services.Interfaces.Grades;

public interface IGradeService
{
    Task<StudentGradesViewModel> GetStudentGradesAsync(
        Guid studentId,
        GradeFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    Task<ClassGradesViewModel> GetClassGradesAsync(
        Guid classId,
        Guid subjectId,
        GradeFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGradeRowDto>> GetStudentRankingAsync(
        Guid classId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetStudentAverageAsync(
        Guid studentId,
        Guid? subjectId = null,
        CancellationToken cancellationToken = default);
}