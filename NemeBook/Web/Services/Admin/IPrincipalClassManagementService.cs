using Entities.Enums;
using Web.ViewModels;

namespace Web.Services.Admin;

public interface IPrincipalClassManagementService
{
    Task<PrincipalClassManagementViewModel?> BuildStudentsViewModelAsync(Guid classId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalStudentSearchResult>> SearchStudentMatchesAsync(string? query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchTeacherMatchesAsync(string? query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchAvailableMainTeacherMatchesAsync(
        Guid classId,
        string? query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchClassSubjectTeacherMatchesAsync(
        Guid subjectId,
        bool includeAllTeachers,
        string? query,
        CancellationToken cancellationToken = default);

    Task AssignMainTeacherAsync(Guid classId, Guid? teacherId, CancellationToken cancellationToken = default);

    Task<PrincipalClassManagementViewModel?> BuildSubjectsViewModelAsync(Guid classId, CancellationToken cancellationToken = default);

    Task<PrincipalSubjectOptionViewModel?> CreateSubjectAsync(string name, CancellationToken cancellationToken = default);

    Task AddClassSubjectAsync(
        Guid classId,
        Guid subjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default);

    Task UpdateClassSubjectTeacherAsync(
        Guid classId,
        Guid classSubjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default);

    Task DeleteClassSubjectAsync(Guid classId, Guid classSubjectId, CancellationToken cancellationToken = default);

    Task<PrincipalClassManagementViewModel?> BuildScheduleViewModelAsync(Guid classId, CancellationToken cancellationToken = default);

    Task<PrincipalScheduleMutationResult> AddScheduleEntryAsync(
        Guid classId,
        DayOfWeek dayOfWeek,
        Guid classSubjectId,
        CancellationToken cancellationToken = default);

    Task<PrincipalScheduleMutationResult> UpdateScheduleEntryAsync(
        Guid classId,
        Guid scheduleEntryId,
        Guid classSubjectId,
        Guid? substituteTeacherId,
        CancellationToken cancellationToken = default);

    Task DeleteScheduleEntryAsync(Guid classId, Guid scheduleEntryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchFreeScheduleTeacherMatchesAsync(
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid? scheduleEntryId,
        Guid? classSubjectId,
        bool includeAllTeachers,
        Guid? excludedTeacherId,
        string? query,
        CancellationToken cancellationToken = default);

    Task<PrincipalClassManagementViewModel?> BuildEventsViewModelAsync(
        Guid classId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<PrincipalEventMutationResult> AddClassEventAsync(
        Guid classId,
        Guid createdByUserId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        CancellationToken cancellationToken = default);

    Task<PrincipalEventMutationResult> UpdateClassEventAsync(
        Guid classId,
        Guid eventId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default);

    Task DeleteClassEventAsync(
        Guid classId,
        Guid eventId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default);

    Task<PrincipalClassManagementViewModel?> BuildPlaceholderViewModelAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken = default);
}

public sealed record PrincipalStudentSearchResult(
    Guid ClassId,
    string FullName,
    string ClassName);

public sealed record PrincipalTeacherSearchResult(
    Guid Id,
    string FullName);

public class PrincipalScheduleMutationResult
{
    public bool NotFound { get; set; }

    public string? Message { get; set; }

    public PrincipalScheduleConflictViewModel? Conflict { get; set; }
}

public class PrincipalEventMutationResult
{
    public bool NotFound { get; set; }

    public string? Message { get; set; }

    public int RedirectYear { get; set; }

    public int RedirectMonth { get; set; }
}
