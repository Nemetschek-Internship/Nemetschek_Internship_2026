using System.Security.Claims;
using Entities.ViewModels.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Grades;
using Services.Interfaces.Parents;
using Services.Interfaces.Students;
using Services.Interfaces.Subjects;

namespace Web.Controllers;

[Authorize]
[Route("Grade")]
public class StudentGradesController : Controller
{
    private readonly IGradeService gradeService;
    private readonly IStudentService studentService;
    private readonly IParentService parentService;
    private readonly IClassSubjectService classSubjectService;
    private readonly ISubjectService subjectService;

    public StudentGradesController(
        IGradeService gradeService,
        IStudentService studentService,
        IParentService parentService,
        IClassSubjectService classSubjectService,
        ISubjectService subjectService)
    {
        this.gradeService = gradeService;
        this.studentService = studentService;
        this.parentService = parentService;
        this.classSubjectService = classSubjectService;
        this.subjectService = subjectService;
    }

    [HttpGet("StudentGrades")]
    [Authorize(Roles = "Teacher,Principal,Parent")]
    public async Task<IActionResult> StudentGrades(Guid studentId, GradeFilterRequest? filter = null)
    {
        if (!await CanAccessStudentGradesAsync(studentId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await gradeService.GetStudentGradesAsync(
            studentId,
            filter,
            CancellationToken.None);

        ViewBag.Subjects = await GetSubjectsForStudentAsync(studentId);
        ViewBag.FromDate = filter?.FromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = filter?.ToDate?.ToString("yyyy-MM-dd");

        return View(viewModel);
    }

    [HttpGet("GetAverage")]
    public async Task<IActionResult> GetAverage(Guid studentId, Guid? subjectId = null)
    {
        var average = await gradeService.GetStudentAverageAsync(
            studentId,
            subjectId,
            CancellationToken.None);

        return Json(new { studentId, subjectId, average });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    private async Task<bool> CanAccessStudentGradesAsync(Guid studentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return false;
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Teacher" || role == "Principal")
        {
            return true;
        }

        if (role == "Parent")
        {
            var parents = await parentService.GetAllAsync();
            var parent = parents.FirstOrDefault(parent => parent.UserId == userId.Value);
            if (parent is not null)
            {
                return parent.Students.Any(student => student.Id == studentId);
            }
        }

        if (role == "Student")
        {
            var students = await studentService.GetAllAsync();
            var student = students.FirstOrDefault(student => student.UserId == userId.Value);
            return student is not null && student.Id == studentId;
        }

        return false;
    }

    private async Task<List<StudentGradeSubjectDto>> GetSubjectsForStudentAsync(Guid studentId)
    {
        var student = await studentService.GetByIdAsync(studentId);
        if (student is null)
        {
            return new List<StudentGradeSubjectDto>();
        }

        var classSubjects = await classSubjectService.GetAllAsync();
        var subjectIds = classSubjects
            .Where(classSubject => classSubject.ClassId == student.ClassId)
            .Select(classSubject => classSubject.SubjectId)
            .Distinct()
            .ToList();

        var subjects = await subjectService.GetAllAsync();
        return subjects
            .Where(subject => subjectIds.Contains(subject.Id))
            .Select(subject => new StudentGradeSubjectDto { Id = subject.Id, Name = subject.Name })
            .ToList();
    }
}

public class StudentGradeSubjectDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
