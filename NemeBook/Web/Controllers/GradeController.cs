using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Classes;
using Services.Interfaces.Grades;
using Services.Interfaces.Parents;
using Services.Interfaces.Students;
using Services.Interfaces.Subjects;
using Services.Interfaces.Teachers;
using Entities.ViewModels.Grades;
using System.Security.Claims;

using Data;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize]
public class GradeController : Controller
{
    private readonly IGradeService _gradeService;
    private readonly IStudentService _studentService;
    private readonly IStudentHomeService _studentHomeService;
    private readonly IClassService _classService;
    private readonly ISubjectService _subjectService;
    private readonly ITeacherService _teacherService;
    private readonly IParentService _parentService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly ILogger<GradeController> _logger;
    private readonly NemeBookDbContext dbContext;

    public GradeController(
        IGradeService gradeService,
        IStudentService studentService,
        IStudentHomeService studentHomeService,
        IClassService classService,
        ISubjectService subjectService,
        ITeacherService teacherService,
        IParentService parentService,
        IClassSubjectService classSubjectService,
        ILogger<GradeController> logger,
        NemeBookDbContext dbContext)
    {
        _gradeService = gradeService;
        _studentService = studentService;
        _studentHomeService = studentHomeService;
        _classService = classService;
        _subjectService = subjectService;
        _teacherService = teacherService;
        _parentService = parentService;
        _classSubjectService = classSubjectService;
        _logger = logger;
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> MyGrades()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var viewModel = await _studentHomeService.GetHomeAsync(userId.Value);
        if (viewModel is null)
            return RedirectToAction("AccessDenied", "Account");

        return View(viewModel);
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
    [Authorize(Roles = "Teacher,Principal,Parent")]
    public async Task<IActionResult> StudentGrades(Guid studentId, GradeFilterRequest? filter = null)
    {
        if (!await CanAccessStudentGradesAsync(studentId))
            return RedirectToAction("AccessDenied", "Account");

        var viewModel = await _gradeService.GetStudentGradesAsync(
            studentId,
            filter,
            CancellationToken.None);

        ViewBag.Subjects = await GetSubjectsForStudentAsync(studentId);
        ViewBag.FromDate = filter?.FromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = filter?.ToDate?.ToString("yyyy-MM-dd");

        return View(viewModel);
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
    [Authorize(Roles = "Principal")]
    public async Task<IActionResult> AllClasses(int grade = 1, CancellationToken cancellationToken = default)
    {
        var selectedGrade = Math.Clamp(grade, 1, 7);

        var classRows = await dbContext.Classes
            .AsNoTracking()
            .Where(schoolClass => schoolClass.GradeNumber == selectedGrade)
            .OrderBy(schoolClass => schoolClass.Letter)
            .Select(schoolClass => new
            {
                schoolClass.Id,
                schoolClass.GradeNumber,
                schoolClass.Letter,
            })
            .ToListAsync(cancellationToken);

        var classIds = classRows.Select(schoolClass => schoolClass.Id).ToList();

        var gradeRows = await dbContext.Grades
            .AsNoTracking()
            .Where(gradeEntry => classIds.Contains(gradeEntry.Student.ClassId))
            .Select(gradeEntry => new
            {
                gradeEntry.Student.ClassId,
                gradeEntry.Value,
            })
            .ToListAsync(cancellationToken);

        var classAverages = gradeRows
            .GroupBy(gradeEntry => gradeEntry.ClassId)
            .ToDictionary(
                group => group.Key,
                group => Math.Round(group.Average(gradeEntry => gradeEntry.Value), 2));

        var viewModel = new PrincipalClassesViewModel
        {
            SelectedGrade = selectedGrade,
            GradeNumbers = Enumerable.Range(1, 7).ToList(),
            Classes = classRows
                .Select(schoolClass => new PrincipalClassCardViewModel
                {
                    Id = schoolClass.Id,
                    GradeNumber = schoolClass.GradeNumber,
                    Letter = schoolClass.Letter,
                    AverageGrade = classAverages.TryGetValue(schoolClass.Id, out var averageGrade)
                        ? averageGrade
                        : null,
                })
                .ToList(),
        };

        return View(viewModel);
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

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }

    private async Task<bool> CanAccessStudentGradesAsync(Guid studentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return false;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Teacher" || role == "Principal")
            return true;

        if (role == "Parent")
        {
            var parents = await _parentService.GetAllAsync();
            var parent = parents.FirstOrDefault(p => p.UserId == userId.Value);
            if (parent is not null)
            {
                return parent.Students.Any(s => s.Id == studentId);
            }
        }

        if (role == "Student")
        {
            var students = await _studentService.GetAllAsync();
            var student = students.FirstOrDefault(s => s.UserId == userId.Value);
            return student is not null && student.Id == studentId;
        }

        return false;
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

    private async Task<List<SubjectDto>> GetSubjectsForStudentAsync(Guid studentId)
    {
        var student = await _studentService.GetByIdAsync(studentId);
        if (student is null) return new List<SubjectDto>();

        var classSubjects = await _classSubjectService.GetAllAsync();
        var subjectIds = classSubjects
            .Where(cs => cs.ClassId == student.ClassId)
            .Select(cs => cs.SubjectId)
            .Distinct()
            .ToList();

        var subjects = await _subjectService.GetAllAsync();
        return subjects
            .Where(s => subjectIds.Contains(s.Id))
            .Select(s => new SubjectDto { Id = s.Id, Name = s.Name })
            .ToList();
    }
}

public class SubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
