using System.Security.Claims;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Interfaces.Teachers;
using Services.Repositories;

namespace Web.Controllers.Teachers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly IAbsenceRepository absenceRepository;
    private readonly IClassSubjectRepository classSubjectRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IGradeRepository gradeRepository;
    private readonly INotificationService notificationService;
    private readonly IStudentRepository studentRepository;
    private readonly ITeacherHomeService teacherHomeService;
    private readonly ITeacherRepository teacherRepository;

    public TeacherController(
        ITeacherHomeService teacherHomeService,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IClassSubjectRepository classSubjectRepository,
        IGradeRepository gradeRepository,
        IAbsenceRepository absenceRepository,
        IFeedbackRepository feedbackRepository,
        INotificationService notificationService)
    {
        this.teacherHomeService = teacherHomeService;
        this.teacherRepository = teacherRepository;
        this.studentRepository = studentRepository;
        this.classSubjectRepository = classSubjectRepository;
        this.gradeRepository = gradeRepository;
        this.absenceRepository = absenceRepository;
        this.feedbackRepository = feedbackRepository;
        this.notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await GetTeacherHomeViewModelAsync(cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MyClass(Guid? classId, CancellationToken cancellationToken)
    {
        var viewModel = await GetTeacherHomeViewModelAsync(
            cancellationToken,
            classId,
            selectDefaultClass: classId.HasValue);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!classId.HasValue)
        {
            return View("MyClasses", viewModel);
        }

        if (viewModel.ClassId != classId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(CancellationToken cancellationToken)
    {
        var viewModel = await GetTeacherHomeViewModelAsync(cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewData["Title"] = "Програма";
        ViewData["ActiveNav"] = "Schedule";
        return View("Placeholder", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(CancellationToken cancellationToken)
    {
        var viewModel = await GetTeacherHomeViewModelAsync(cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewData["Title"] = "Календар";
        ViewData["ActiveNav"] = "Calendar";
        return View("Placeholder", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGrade(
        TeacherGradeRecordInputModel model,
        CancellationToken cancellationToken)
    {
        if (model.Value < 2 || model.Value > 6 || !Enum.IsDefined(model.Type))
        {
            return RedirectToAction(nameof(MyClass), new { classId = model.ClassId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var grade = new Grade
        {
            Id = Guid.NewGuid(),
            StudentId = access.Student.Id,
            ClassSubjectId = access.ClassSubject.Id,
            Value = model.Value,
            Type = model.Type,
            Note = model.Note?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        await gradeRepository.CreateAsync(grade, cancellationToken);
        await notificationService.CreateNotificationAsync(
            access.Student.UserId,
            NotificationType.Grade,
            $"Нова оценка {grade.Value:0.##} по {access.ClassSubject.Subject.Name}.",
            gradeId: grade.Id,
            cancellationToken: cancellationToken);

        return RedirectToAction(nameof(MyClass), new { classId = access.Student.ClassId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAbsence(
        TeacherAbsenceRecordInputModel model,
        CancellationToken cancellationToken)
    {
        if (model.LessonNumber < 1 || !Enum.IsDefined(model.Type) || !Enum.IsDefined(model.Status))
        {
            return RedirectToAction(nameof(MyClass), new { classId = model.ClassId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            StudentId = access.Student.Id,
            ClassSubjectId = access.ClassSubject.Id,
            Date = model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date,
            LessonNumber = model.LessonNumber,
            Type = model.Type,
            Status = model.Status,
            ExcuseReason = model.Status == AbsenceStatus.Excused ? model.ExcuseReason : null,
            ExcuseNote = model.ExcuseNote?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        await absenceRepository.CreateAsync(absence, cancellationToken);
        await notificationService.CreateNotificationAsync(
            access.Student.UserId,
            NotificationType.Absence,
            $"Ново отсъствие по {access.ClassSubject.Subject.Name}.",
            absenceId: absence.Id,
            cancellationToken: cancellationToken);

        return RedirectToAction(nameof(MyClass), new { classId = access.Student.ClassId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFeedback(
        TeacherFeedbackRecordInputModel model,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(model.Type) || string.IsNullOrWhiteSpace(model.Description))
        {
            return RedirectToAction(nameof(MyClass), new { classId = model.ClassId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            StudentId = access.Student.Id,
            ClassSubjectId = access.ClassSubject.Id,
            Date = model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date,
            Type = model.Type,
            Description = model.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await feedbackRepository.CreateAsync(feedback, cancellationToken);
        await notificationService.CreateNotificationAsync(
            access.Student.UserId,
            NotificationType.Feedback,
            $"Нов отзив по {access.ClassSubject.Subject.Name}.",
            feedbackId: feedback.Id,
            cancellationToken: cancellationToken);

        return RedirectToAction(nameof(MyClass), new { classId = access.Student.ClassId });
    }

    private async Task<Entities.ViewModels.Teachers.TeacherHomeViewModel?> GetTeacherHomeViewModelAsync(
        CancellationToken cancellationToken,
        Guid? classId = null,
        bool selectDefaultClass = true)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        return await teacherHomeService.GetHomeAsync(userId.Value, classId, selectDefaultClass, cancellationToken);
    }

    private async Task<TeacherStudentRecordAccess?> GetTeacherClassSubjectForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            return null;
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == userId.Value);
        if (teacher is null)
        {
            return null;
        }

        var student = await studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return null;
        }

        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var classSubject = classSubjects
            .OrderBy(currentClassSubject => currentClassSubject.Subject.Name)
            .FirstOrDefault(currentClassSubject =>
                currentClassSubject.ClassId == student.ClassId &&
                currentClassSubject.TeacherId == teacher.Id);

        return classSubject is null
            ? null
            : new TeacherStudentRecordAccess(student, classSubject);
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

    private sealed record TeacherStudentRecordAccess(Student Student, ClassSubject ClassSubject);
}

public class TeacherGradeRecordInputModel
{
    public Guid? ClassId { get; set; }

    public Guid StudentId { get; set; }

    public decimal Value { get; set; }

    public GradeType Type { get; set; }

    public string? Note { get; set; }
}

public class TeacherAbsenceRecordInputModel
{
    public Guid? ClassId { get; set; }

    public Guid StudentId { get; set; }

    public DateOnly Date { get; set; }

    public int LessonNumber { get; set; }

    public AbsenceType Type { get; set; }

    public AbsenceStatus Status { get; set; }

    public AbsenceExcuseReason? ExcuseReason { get; set; }

    public string? ExcuseNote { get; set; }
}

public class TeacherFeedbackRecordInputModel
{
    public Guid? ClassId { get; set; }

    public Guid StudentId { get; set; }

    public DateOnly Date { get; set; }

    public FeedbackType Type { get; set; }

    public string Description { get; set; } = string.Empty;
}
