using Web.ViewModels;

namespace Web.Services.Admin;

public interface IPrincipalReportsService
{
    Task<PrincipalReportsViewModel> BuildReportAsync(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default);

    Task<PrincipalReportExportFile> ExportReportAsync(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalReportStudentSearchResult>> SearchStudentMatchesAsync(
        string? query,
        Guid? classId,
        CancellationToken cancellationToken = default);
}
