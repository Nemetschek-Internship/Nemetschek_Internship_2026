using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entities.ViewModels.Grades;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Classes;
using Services.Interfaces.Grades;
using Services.Interfaces.Subjects;
using Services.Interfaces.Teachers;
using System.Security.Claims;

namespace Web.Controllers;

[Authorize]
public class GradeController : Controller
{
    private readonly IGradeService _gradeService;
    private readonly IClassService _classService;
    private readonly ISubjectService _subjectService;
    private readonly ITeacherService _teacherService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly ILogger<GradeController> _logger;

    public GradeController(
        IGradeService gradeService,
        IClassService classService,
        ISubjectService subjectService,
        ITeacherService teacherService,
        IClassSubjectService classSubjectService,
        ILogger<GradeController> logger)
    {
        _gradeService = gradeService;
        _classService = classService;
        _subjectService = subjectService;
        _teacherService = teacherService;
        _classSubjectService = classSubjectService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult MyGrades()
    {
        return RedirectToAction("MyGrades", "Student");
    }

    private async Task SetTeacherViewBagDataAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Teacher" || role == "Principal")
        {
            var teachers = await _teacherService.GetAllAsync();
            var teacher = teachers.FirstOrDefault(t => t.UserId == userId.Value);

            if (teacher is not null)
            {
                var classSubjects = await _classSubjectService.GetAllAsync();
                var classSubject = classSubjects.FirstOrDefault(cs => cs.TeacherId == teacher.Id);

                if (classSubject is not null)
                {
                    ViewBag.ClassId = classSubject.ClassId;
                    ViewBag.SubjectId = classSubject.SubjectId;
                }
            }
        }
    }
    [HttpGet]
    [Authorize(Roles = "Teacher,Principal")]
    public async Task<IActionResult> ClassGrades(Guid classId, Guid subjectId, GradeFilterRequest? filter = null)
    {
        if (!await CanAccessClassGradesAsync(classId, subjectId))
            return RedirectToAction("AccessDenied", "Account");

        var viewModel = await _gradeService.GetClassGradesAsync(
            classId,
            subjectId,
            filter,
            CancellationToken.None);

        ViewBag.FromDate = filter?.FromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = filter?.ToDate?.ToString("yyyy-MM-dd");

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = "Teacher,Principal")]
    public async Task<IActionResult> Ranking(Guid classId, Guid subjectId)
    {
        if (!await CanAccessClassGradesAsync(classId, subjectId))
            return RedirectToAction("AccessDenied", "Account");

        var ranking = await _gradeService.GetStudentRankingAsync(
            classId,
            subjectId,
            CancellationToken.None);

        var classEntity = await _classService.GetByIdAsync(classId);
        var subject = await _subjectService.GetByIdAsync(subjectId);

        ViewBag.ClassName = classEntity is not null
            ? $"{classEntity.GradeNumber}{classEntity.Letter}"
            : "Неизвестно";
        ViewBag.SubjectName = subject?.Name ?? "Неизвестно";

        return View(ranking);
    }

    [HttpGet]
    public async Task<IActionResult> GetAverage(Guid studentId, Guid? subjectId = null)
    {
        var average = await _gradeService.GetStudentAverageAsync(
            studentId,
            subjectId,
            CancellationToken.None);

        return Json(new { studentId, subjectId, average });
    }

    [HttpGet]
[Authorize(Roles = "Teacher,Principal")]
public async Task<IActionResult> SelectClassAndSubject()
{
    var userId = GetCurrentUserId();
    if (!userId.HasValue)
        return RedirectToAction("Login", "Account");

    var teachers = await _teacherService.GetAllAsync();
    var teacher = teachers.FirstOrDefault(t => t.UserId == userId.Value);
    
    if (teacher is null)
        return RedirectToAction("AccessDenied", "Account");

    var classSubjects = await _classSubjectService.GetAllAsync();
    var teacherClassSubjects = classSubjects
        .Where(cs => cs.TeacherId == teacher.Id)
        .ToList();

    var classes = await _classService.GetAllAsync();
    var subjects = await _subjectService.GetAllAsync();

    var viewModel = new SelectClassSubjectViewModel
    {
        TeacherId = teacher.Id,
        TeacherName = $"{teacher.User.FirstName} {teacher.User.LastName}",
        AvailableClasses = classes
            .Where(c => teacherClassSubjects.Any(tcs => tcs.ClassId == c.Id))
            .Select(c => new ClassSelectDto { Id = c.Id, Name = $"{c.GradeNumber}{c.Letter}" })
            .ToList(),
        AvailableSubjects = subjects
            .Where(s => teacherClassSubjects.Any(tcs => tcs.SubjectId == s.Id))
            .Select(s => new SubjectSelectDto { Id = s.Id, Name = s.Name })
            .ToList()
    };

    return View(viewModel);
}

[HttpPost]
[Authorize(Roles = "Teacher,Principal")]
public IActionResult SelectClassAndSubject(Guid classId, Guid subjectId)
{
    return RedirectToAction("ClassGrades", new { classId, subjectId });
}

[HttpPost]
[Authorize(Roles = "Teacher,Principal")]
// TODO: Replace [IgnoreAntiforgeryToken] with header-based CSRF
// protection (X-CSRF-TOKEN) before merging this branch into dev.
// Temporarily disabled only to allow direct Postman testing during
// development — this is a tracked, intentional gap, not an oversight.
// Tracked in the team to-do list under point 2 (Grades).
[IgnoreAntiforgeryToken]
public async Task<IActionResult> CreateGrade([FromBody] CreateGradeRequest request, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var userId = GetCurrentUserId();
    if (!userId.HasValue)
        return Unauthorized();

    try
    {
        var gradeDto = await _gradeService.CreateGradeAsync(request, userId.Value, cancellationToken);
        return Ok(gradeDto);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "User {UserId} was denied creating a grade for class subject {ClassSubjectId}", userId, request.ClassSubjectId);
        return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        _logger.LogWarning(ex, "Grade creation failed for student {StudentId}", request.StudentId);
        return BadRequest(new { error = ex.Message });
    }
}

[HttpPost]
[Authorize(Roles = "Teacher,Principal")]
// TODO: Replace [IgnoreAntiforgeryToken] with header-based CSRF
// protection (X-CSRF-TOKEN) before merging this branch into dev.
// Same tracked gap as CreateGrade — see team to-do list, point 2.
[IgnoreAntiforgeryToken]
public async Task<IActionResult> CreateGradesBulk([FromBody] BulkCreateGradeRequest request, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var userId = GetCurrentUserId();
    if (!userId.HasValue)
        return Unauthorized();

    try
    {
        var result = await _gradeService.CreateGradesBulkAsync(request, userId.Value, cancellationToken);

        if (result.CreatedGrades.Count == 0)
            return BadRequest(new { error = "Нито една от избраните оценки не можа да бъде записана.", errors = result.Errors });

        return Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "User {UserId} was denied bulk-creating grades for class subject {ClassSubjectId}", userId, request.ClassSubjectId);
        return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
    }
    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
    {
        _logger.LogWarning(ex, "Bulk grade creation failed for class subject {ClassSubjectId}", request.ClassSubjectId);
        return BadRequest(new { error = ex.Message });
    }
}

[HttpPut]
[Authorize(Roles = "Teacher,Principal")]
// TODO: Replace [IgnoreAntiforgeryToken] with header-based CSRF
// protection (X-CSRF-TOKEN) before merging this branch into dev.
// Same tracked gap as CreateGrade/CreateGradesBulk — see team to-do
// list, point 2.
[IgnoreAntiforgeryToken]
public async Task<IActionResult> UpdateGrade([FromBody] UpdateGradeRequest request, CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var userId = GetCurrentUserId();
    if (!userId.HasValue)
        return Unauthorized();

    var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    try
    {
        var gradeDto = await _gradeService.UpdateGradeAsync(request, userId.Value, role, cancellationToken);
        return Ok(gradeDto);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or ArgumentException)
    {
        _logger.LogWarning(ex, "Grade update failed for grade {GradeId}", request.GradeId);
        return BadRequest(new { error = ex.Message });
    }
}

[HttpDelete]
[Authorize(Roles = "Teacher,Principal")]
// TODO: Replace [IgnoreAntiforgeryToken] with header-based CSRF
// protection (X-CSRF-TOKEN) before merging this branch into dev.
// Same tracked gap as CreateGrade/CreateGradesBulk/UpdateGrade — see
// team to-do list, point 2.
[IgnoreAntiforgeryToken]
public async Task<IActionResult> DeleteGrade(Guid gradeId, CancellationToken cancellationToken)
{
    var userId = GetCurrentUserId();
    if (!userId.HasValue)
        return Unauthorized();

    var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    try
    {
        await _gradeService.DeleteGradeAsync(gradeId, userId.Value, role, cancellationToken);
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or ArgumentException)
    {
        _logger.LogWarning(ex, "Grade deletion failed for grade {GradeId}", gradeId);
        return BadRequest(new { error = ex.Message });
    }
}

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }

    private async Task<bool> CanAccessClassGradesAsync(Guid classId, Guid subjectId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return false;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Principal") return true;

        if (role == "Teacher")
        {
            var teachers = await _teacherService.GetAllAsync();
            var teacher = teachers.FirstOrDefault(t => t.UserId == userId.Value);
            if (teacher is null) return false;

            var classSubjects = await _classSubjectService.GetAllAsync();
            return classSubjects.Any(cs =>
                cs.ClassId == classId &&
                cs.SubjectId == subjectId &&
                cs.TeacherId == teacher.Id);
        }

        return false;
    }

}

public class SelectClassSubjectViewModel
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public List<ClassSelectDto> AvailableClasses { get; set; } = new();
    public List<SubjectSelectDto> AvailableSubjects { get; set; } = new();
}

public class ClassSelectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SubjectSelectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
