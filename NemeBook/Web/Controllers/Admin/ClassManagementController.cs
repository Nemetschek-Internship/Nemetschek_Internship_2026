using Entities.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Admin;
using Web.ViewModels;

namespace Web.Controllers.Admin;

[Route("Admin/[controller]/[action]")]
[Authorize(Roles = "Principal")]
public class ClassManagementController : Controller
{
    private readonly IPrincipalClassManagementService classManagementService;

    public ClassManagementController(IPrincipalClassManagementService classManagementService)
    {
        this.classManagementService = classManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Students(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await classManagementService.BuildStudentsViewModelAsync(classId, cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SearchStudentMatches(string? query, CancellationToken cancellationToken = default)
    {
        var matches = await classManagementService.SearchStudentMatchesAsync(query, cancellationToken);

        return Json(matches.Select(match => new
        {
            fullName = match.FullName,
            className = match.ClassName,
            url = Url.Action("Student", "Teacher", new { area = "", studentId = match.StudentId }),
        }));
    }

    [HttpGet]
    public async Task<IActionResult> SearchTeacherMatches(string? query, CancellationToken cancellationToken = default)
    {
        var matches = await classManagementService.SearchTeacherMatchesAsync(query, cancellationToken);
        return Json(ToTeacherJson(matches));
    }

    [HttpGet]
    public async Task<IActionResult> SearchAvailableMainTeacherMatches(
        Guid classId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var matches = await classManagementService.SearchAvailableMainTeacherMatchesAsync(
            classId,
            query,
            cancellationToken);

        return Json(ToTeacherJson(matches));
    }

    [HttpGet]
    public async Task<IActionResult> SearchClassSubjectTeacherMatches(
        Guid subjectId,
        bool includeAllTeachers,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var matches = await classManagementService.SearchClassSubjectTeacherMatchesAsync(
            subjectId,
            includeAllTeachers,
            query,
            cancellationToken);

        return Json(ToTeacherJson(matches));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMainTeacher(
        Guid classId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.AssignMainTeacherAsync(classId, teacherId, cancellationToken);
        return RedirectToAction(nameof(Students), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> Subjects(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await classManagementService.BuildSubjectsViewModelAsync(classId, cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string name, CancellationToken cancellationToken = default)
    {
        var subject = await classManagementService.CreateSubjectAsync(name, cancellationToken);

        if (subject is null)
        {
            return BadRequest();
        }

        return Json(new
        {
            id = subject.Id,
            name = subject.Name,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClassSubject(
        Guid classId,
        Guid subjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.AddClassSubjectAsync(classId, subjectId, teacherId, cancellationToken);
        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClassSubjectTeacher(
        Guid classId,
        Guid classSubjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.UpdateClassSubjectTeacherAsync(
            classId,
            classSubjectId,
            teacherId,
            cancellationToken);

        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClassSubject(
        Guid classId,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.DeleteClassSubjectAsync(classId, classSubjectId, cancellationToken);
        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await classManagementService.BuildScheduleViewModelAsync(classId, cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScheduleEntry(
        Guid classId,
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        _ = periodNumber;

        var result = await classManagementService.AddScheduleEntryAsync(
            classId,
            dayOfWeek,
            classSubjectId,
            cancellationToken);

        return await HandleScheduleMutationResultAsync(classId, result, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScheduleEntry(
        Guid classId,
        Guid scheduleEntryId,
        Guid classSubjectId,
        Guid? substituteTeacherId,
        CancellationToken cancellationToken = default)
    {
        var result = await classManagementService.UpdateScheduleEntryAsync(
            classId,
            scheduleEntryId,
            classSubjectId,
            substituteTeacherId,
            cancellationToken);

        return await HandleScheduleMutationResultAsync(classId, result, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScheduleEntry(
        Guid classId,
        Guid scheduleEntryId,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.DeleteScheduleEntryAsync(classId, scheduleEntryId, cancellationToken);
        return RedirectToAction(nameof(Schedule), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchFreeScheduleTeacherMatches(
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid? scheduleEntryId,
        Guid? classSubjectId,
        bool includeAllTeachers,
        Guid? excludedTeacherId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var matches = await classManagementService.SearchFreeScheduleTeacherMatchesAsync(
            dayOfWeek,
            periodNumber,
            scheduleEntryId,
            classSubjectId,
            includeAllTeachers,
            excludedTeacherId,
            query,
            cancellationToken);

        return Json(ToTeacherJson(matches));
    }

    [HttpGet]
    public async Task<IActionResult> Events(
        Guid classId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = GetSelectedEventsMonth(year, month);
        var viewModel = await classManagementService.BuildEventsViewModelAsync(
            classId,
            selectedDate.Year,
            selectedDate.Month,
            cancellationToken);

        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClassEvent(
        Guid classId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        var result = await classManagementService.AddClassEventAsync(
            classId,
            currentUserId.Value,
            title,
            description,
            eventType,
            date,
            classSubjectId,
            cancellationToken);

        return HandleEventMutationResult(classId, result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClassEvent(
        Guid classId,
        Guid eventId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default)
    {
        var result = await classManagementService.UpdateClassEventAsync(
            classId,
            eventId,
            title,
            description,
            eventType,
            date,
            classSubjectId,
            returnYear,
            returnMonth,
            cancellationToken);

        return HandleEventMutationResult(classId, result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClassEvent(
        Guid classId,
        Guid eventId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default)
    {
        await classManagementService.DeleteClassEventAsync(
            classId,
            eventId,
            returnYear,
            returnMonth,
            cancellationToken);

        return RedirectToAction(nameof(Events), new { classId, year = returnYear, month = returnMonth });
    }

    [HttpGet]
    public async Task<IActionResult> Placeholder(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await classManagementService.BuildPlaceholderViewModelAsync(
            classId,
            activeTab,
            sectionTitle,
            cancellationToken);

        return viewModel is null ? NotFound() : View("Placeholder", viewModel);
    }

    private async Task<IActionResult> HandleScheduleMutationResultAsync(
        Guid classId,
        PrincipalScheduleMutationResult result,
        CancellationToken cancellationToken)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            TempData["ScheduleMessage"] = result.Message;
            return RedirectToAction(nameof(Schedule), new { classId });
        }

        if (result.Conflict is not null)
        {
            return await ViewScheduleWithConflictAsync(classId, result.Conflict, cancellationToken);
        }

        return RedirectToAction(nameof(Schedule), new { classId });
    }

    private async Task<IActionResult> ViewScheduleWithConflictAsync(
        Guid classId,
        PrincipalScheduleConflictViewModel conflict,
        CancellationToken cancellationToken)
    {
        var viewModel = await classManagementService.BuildScheduleViewModelAsync(classId, cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        viewModel.ScheduleConflict = conflict;
        return View("Schedule", viewModel);
    }

    private IActionResult HandleEventMutationResult(Guid classId, PrincipalEventMutationResult result)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            TempData["ClassEventMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Events), new
        {
            classId,
            year = result.RedirectYear,
            month = result.RedirectMonth,
        });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private static DateTime GetSelectedEventsMonth(int? year, int? month)
    {
        var today = DateTime.Today;

        if (!year.HasValue ||
            !month.HasValue ||
            year.Value < 1900 ||
            month.Value is < 1 or > 12)
        {
            return new DateTime(today.Year, today.Month, 1);
        }

        return new DateTime(year.Value, month.Value, 1);
    }

    private static IEnumerable<object> ToTeacherJson(IEnumerable<PrincipalTeacherSearchResult> matches)
    {
        return matches.Select(match => new
        {
            id = match.Id,
            fullName = match.FullName,
        });
    }
}
