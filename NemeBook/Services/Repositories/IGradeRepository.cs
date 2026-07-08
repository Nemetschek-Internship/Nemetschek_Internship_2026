using Entities.Enums;
using Entities.Models;

namespace Services.Repositories;

public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Grade>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Grade grade, CancellationToken cancellationToken = default);
    Task UpdateAsync(Grade grade, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetGradesByStudentIdAsync(
        Guid studentId,
        GradeFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetGradesByClassSubjectIdAsync(
        Guid classSubjectId,
        GradeFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetGradesByStudentAndSubjectIdAsync(
        Guid studentId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetAveragesByClassSubjectIdAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default);

    Task<GradeStatisticsDto> GetStatisticsByClassSubjectIdAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default);
}

public class GradeFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public GradeType? Type { get; set; }
    public int? MaxCount { get; set; }
}

public class GradeStatisticsDto
{
    public int TotalGrades { get; set; }
    public decimal Average { get; set; }
    public decimal? MinGrade { get; set; }
    public decimal? MaxGrade { get; set; }
    public Dictionary<int, int> GradeDistribution { get; set; } = new();
}