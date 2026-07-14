using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Interfaces.Teachers;
using Services.Repositories;

namespace Web.Controllers.Teachers;

[Authorize(Roles = "Teacher,Principal")]
public class TeacherController : Controller
{
    private readonly IAbsenceRepository absenceRepository;
    private readonly IClassRepository classRepository;
    private readonly IClassScheduleEntryRepository classScheduleEntryRepository;
    private readonly IClassSubjectRepository classSubjectRepository;
    private readonly IEventRepository eventRepository;
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
        IClassRepository classRepository,
        IClassSubjectRepository classSubjectRepository,
        IClassScheduleEntryRepository classScheduleEntryRepository,
        IGradeRepository gradeRepository,
        IAbsenceRepository absenceRepository,
        IFeedbackRepository feedbackRepository,
        IEventRepository eventRepository,
        INotificationService notificationService)
    {
        this.teacherHomeService = teacherHomeService;
        this.teacherRepository = teacherRepository;
        this.studentRepository = studentRepository;
        this.classRepository = classRepository;
        this.classSubjectRepository = classSubjectRepository;
        this.classScheduleEntryRepository = classScheduleEntryRepository;
        this.gradeRepository = gradeRepository;
        this.absenceRepository = absenceRepository;
        this.feedbackRepository = feedbackRepository;
        this.eventRepository = eventRepository;
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
    public async Task<IActionResult> MyClass(Guid? classId, Guid? classSubjectId, CancellationToken cancellationToken)
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
            return viewModel.MainClassId.HasValue
                ? RedirectToAction(nameof(MyClass), new { classId = viewModel.MainClassId.Value })
                : RedirectToAction(nameof(MySubjects));
        }

        if (viewModel.ClassId != classId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewData["SelectedClassSubjectId"] = classSubjectId;
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MySubjects(CancellationToken cancellationToken)
    {
        var viewModel = await GetTeacherHomeViewModelAsync(
            cancellationToken,
            classId: null,
            selectDefaultClass: false);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Student(Guid studentId, CancellationToken cancellationToken)
    {
        var access = await GetStudentProfileAccessAsync(studentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await BuildStudentDetailsViewModelAsync(access, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> StudentFeedbacks(Guid studentId, CancellationToken cancellationToken)
    {
        var access = await GetStudentProfileAccessAsync(studentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await BuildStudentFeedbacksViewModelAsync(access, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> StudentAbsences(Guid studentId, CancellationToken cancellationToken)
    {
        var access = await GetStudentProfileAccessAsync(studentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await BuildStudentAbsencesViewModelAsync(access, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(CancellationToken cancellationToken)
    {
        var homeViewModel = await GetTeacherHomeViewModelAsync(cancellationToken);
        if (homeViewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await BuildScheduleViewModelAsync(homeViewModel, cancellationToken);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(int? year, int? month, CancellationToken cancellationToken)
    {
        var homeViewModel = await GetTeacherHomeViewModelAsync(cancellationToken);
        if (homeViewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var viewModel = await BuildCalendarViewModelAsync(homeViewModel, year, month, cancellationToken);
        return View(viewModel);
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

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, model.ClassSubjectId, cancellationToken);
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
    public async Task<IActionResult> UpdateStudentGrade(
        TeacherGradeEditInputModel model,
        CancellationToken cancellationToken)
    {
        var grade = await gradeRepository.GetByIdAsync(model.GradeId, cancellationToken);
        if (grade is null || model.Value < 2 || model.Value > 6 || !Enum.IsDefined(model.Type))
        {
            return RedirectToAction(nameof(Student), new { studentId = model.StudentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(grade.StudentId, grade.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!User.IsInRole("Principal") && DateTime.UtcNow - grade.CreatedAt > TimeSpan.FromDays(7))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        grade.Value = model.Value;
        grade.Type = model.Type;
        grade.Note = model.Note?.Trim() ?? string.Empty;

        await gradeRepository.UpdateAsync(grade, cancellationToken);
        return RedirectToAction(nameof(Student), new { studentId = grade.StudentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudentGrade(
        Guid gradeId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var grade = await gradeRepository.GetByIdAsync(gradeId, cancellationToken);
        if (grade is null)
        {
            return RedirectToAction(nameof(Student), new { studentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(grade.StudentId, grade.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!User.IsInRole("Principal") && DateTime.UtcNow - grade.CreatedAt > TimeSpan.FromDays(7))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        await gradeRepository.DeleteAsync(grade.Id, cancellationToken);
        return RedirectToAction(nameof(Student), new { studentId = grade.StudentId });
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

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, model.ClassSubjectId, cancellationToken);
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
    public async Task<IActionResult> UpdateStudentAbsence(
        TeacherAbsenceEditInputModel model,
        CancellationToken cancellationToken)
    {
        var absence = await absenceRepository.GetByIdAsync(model.AbsenceId, cancellationToken);
        if (absence is null ||
            model.LessonNumber < 1 ||
            !Enum.IsDefined(model.Type) ||
            !Enum.IsDefined(model.Status))
        {
            return RedirectToAction(nameof(Student), new { studentId = model.StudentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(absence.StudentId, absence.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        absence.Date = model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date;
        absence.LessonNumber = model.LessonNumber;
        absence.Type = model.Type;
        absence.Status = model.Status;
        absence.ExcuseReason = model.Status == AbsenceStatus.Excused ? model.ExcuseReason : null;
        absence.ExcuseNote = model.Status == AbsenceStatus.Excused
            ? model.ExcuseNote?.Trim() ?? string.Empty
            : string.Empty;

        await absenceRepository.UpdateAsync(absence, cancellationToken);
        return RedirectToAction(nameof(Student), new { studentId = absence.StudentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudentAbsence(
        Guid absenceId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var absence = await absenceRepository.GetByIdAsync(absenceId, cancellationToken);
        if (absence is null)
        {
            return RedirectToAction(nameof(Student), new { studentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(absence.StudentId, absence.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        await absenceRepository.DeleteAsync(absence.Id, cancellationToken);
        return RedirectToAction(nameof(Student), new { studentId = absence.StudentId });
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

        var access = await GetTeacherClassSubjectForStudentAsync(model.StudentId, model.ClassSubjectId, cancellationToken);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStudentFeedback(
        TeacherFeedbackEditInputModel model,
        CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(model.FeedbackId, cancellationToken);
        if (feedback is null || !Enum.IsDefined(model.Type) || string.IsNullOrWhiteSpace(model.Description))
        {
            return RedirectToAction(nameof(Student), new { studentId = model.StudentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(feedback.StudentId, feedback.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        feedback.Date = model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date;
        feedback.Type = model.Type;
        feedback.Description = model.Description.Trim();

        await feedbackRepository.UpdateAsync(feedback, cancellationToken);
        return RedirectToAction(nameof(StudentFeedbacks), new { studentId = feedback.StudentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudentFeedback(
        Guid feedbackId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(feedbackId, cancellationToken);
        if (feedback is null)
        {
            return RedirectToAction(nameof(Student), new { studentId });
        }

        var access = await GetTeacherClassSubjectForStudentAsync(feedback.StudentId, feedback.ClassSubjectId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        await feedbackRepository.DeleteAsync(feedback.Id, cancellationToken);
        return RedirectToAction(nameof(StudentFeedbacks), new { studentId = feedback.StudentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcuseAbsence(
        TeacherExcuseAbsenceInputModel model,
        CancellationToken cancellationToken)
    {
        var absence = await absenceRepository.GetByIdAsync(model.AbsenceId, cancellationToken);
        if (absence is null)
        {
            return RedirectToAction(nameof(MyClass));
        }

        var access = await GetStudentProfileAccessAsync(absence.StudentId, cancellationToken);
        if (access is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!Enum.IsDefined(model.ExcuseReason))
        {
            model.ExcuseReason = AbsenceExcuseReason.Other;
        }

        absence.Status = AbsenceStatus.Excused;
        absence.ExcuseReason = model.ExcuseReason;
        absence.ExcuseNote = model.ExcuseNote?.Trim() ?? string.Empty;

        await absenceRepository.UpdateAsync(absence, cancellationToken);
        await notificationService.CreateNotificationAsync(
            access.Student.UserId,
            NotificationType.Absence,
            "Отсъствието ти беше извинено от класния ръководител.",
            absenceId: absence.Id,
            cancellationToken: cancellationToken);

        return RedirectToAction(nameof(StudentAbsences), new { studentId = access.Student.Id });
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
        Guid classSubjectId,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty || classSubjectId == Guid.Empty)
        {
            return null;
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        var student = await studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null || !student.User.IsActive || student.User.IsDeleted)
        {
            return null;
        }

        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var classSubject = classSubjects
            .FirstOrDefault(currentClassSubject =>
                currentClassSubject.Id == classSubjectId &&
                currentClassSubject.ClassId == student.ClassId);

        if (classSubject is null)
        {
            return null;
        }

        if (!User.IsInRole("Principal"))
        {
            var teachers = await teacherRepository.GetAllAsync(cancellationToken);
            var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == userId.Value);
            if (teacher is null || classSubject.TeacherId != teacher.Id)
            {
                return null;
            }
        }

        return new TeacherStudentRecordAccess(student, classSubject);
    }

    private async Task<TeacherMainStudentAccess?> GetStudentProfileAccessAsync(
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

        var student = await studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null || student.User.IsDeleted || !student.User.IsActive)
        {
            return null;
        }

        var schoolClass = await classRepository.GetByIdAsync(student.ClassId, cancellationToken);
        if (schoolClass is null)
        {
            return null;
        }

        if (User.IsInRole("Principal"))
        {
            return new TeacherMainStudentAccess(null, student, schoolClass, true);
        }

        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == userId.Value);
        if (teacher is null || schoolClass.MainTeacherId != teacher.Id)
        {
            return null;
        }

        return new TeacherMainStudentAccess(teacher, student, schoolClass, false);
    }

    private async Task<TeacherStudentDetailsViewModel> BuildStudentDetailsViewModelAsync(
        TeacherMainStudentAccess access,
        CancellationToken cancellationToken)
    {
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var subjectByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == access.Student.ClassId)
            .ToDictionary(classSubject => classSubject.Id, classSubject => classSubject.Subject.Name);
        var teacherByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == access.Student.ClassId)
            .ToDictionary(
                classSubject => classSubject.Id,
                classSubject => classSubject.Teacher?.User is null
                    ? "Няма назначен учител"
                    : FormatPersonName(classSubject.Teacher.User));

        var grades = (await gradeRepository.GetGradesByStudentIdAsync(access.Student.Id, cancellationToken: cancellationToken))
            .Select(grade => new TeacherStudentGradeDetailsItem
            {
                Id = grade.Id,
                ClassSubjectId = grade.ClassSubjectId,
                SubjectName = GetSubjectName(grade.ClassSubjectId, subjectByClassSubjectId),
                TeacherName = GetSubjectName(grade.ClassSubjectId, teacherByClassSubjectId),
                Value = grade.Value,
                DisplayValue = GetRoundedGrade(grade.Value),
                TypeValue = (int)grade.Type,
                TypeName = GetDisplayName(grade.Type),
                Note = grade.Note,
                CreatedAt = grade.CreatedAt
            })
            .ToArray();

        var feedbacks = MapStudentFeedbacks(access.Student.Id, subjectByClassSubjectId, await feedbackRepository.GetAllAsync(cancellationToken))
            .ToArray();

        var absences = (await absenceRepository.GetAllAsync(cancellationToken))
            .Where(absence => absence.StudentId == access.Student.Id)
            .OrderByDescending(absence => absence.Date)
            .ThenByDescending(absence => absence.LessonNumber)
            .Select(absence => new TeacherStudentAbsenceDetailsItem
            {
                Id = absence.Id,
                ClassSubjectId = absence.ClassSubjectId,
                SubjectName = GetSubjectName(absence.ClassSubjectId, subjectByClassSubjectId),
                Date = absence.Date,
                LessonNumber = absence.LessonNumber,
                TypeValue = (int)absence.Type,
                TypeName = GetDisplayName(absence.Type),
                StatusValue = (int)absence.Status,
                StatusName = GetDisplayName(absence.Status),
                IsExcused = absence.Status == AbsenceStatus.Excused,
                ExcuseReasonValue = absence.ExcuseReason.HasValue ? (int)absence.ExcuseReason.Value : null,
                ExcuseReasonName = absence.ExcuseReason.HasValue ? GetDisplayName(absence.ExcuseReason.Value) : string.Empty,
                ExcuseNote = absence.ExcuseNote,
                CanExcuse = true
            })
            .ToArray();

        return new TeacherStudentDetailsViewModel
        {
            TeacherName = access.IsPrincipal ? "Директор" : FormatPersonName(access.Teacher!.User),
            TeacherInitials = access.IsPrincipal ? "Д" : GetInitials(access.Teacher!.User),
            MainMeta = access.IsPrincipal
                ? $"Администратор · клас {FormatClassName(access.SchoolClass)}"
                : $"Класен ръководител на {FormatClassName(access.SchoolClass)}",
            StudentId = access.Student.Id,
            ClassId = access.SchoolClass.Id,
            StudentName = FormatPersonName(access.Student.User),
            StudentInitials = GetInitials(access.Student.User),
            ClassName = FormatClassName(access.SchoolClass),
            CanManageRecords = true,
            Grades = grades,
            Feedbacks = feedbacks,
            Absences = absences
        };
    }

    private async Task<TeacherStudentFeedbacksViewModel> BuildStudentFeedbacksViewModelAsync(
        TeacherMainStudentAccess access,
        CancellationToken cancellationToken)
    {
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var subjectByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == access.Student.ClassId)
            .ToDictionary(classSubject => classSubject.Id, classSubject => classSubject.Subject.Name);
        var manageableClassSubjectIds = GetManageableClassSubjectIds(
            access,
            classSubjects.Where(classSubject => classSubject.ClassId == access.Student.ClassId));

        return new TeacherStudentFeedbacksViewModel
        {
            TeacherName = access.IsPrincipal ? "Директор" : FormatPersonName(access.Teacher!.User),
            TeacherInitials = access.IsPrincipal ? "Д" : GetInitials(access.Teacher!.User),
            MainMeta = access.IsPrincipal
                ? $"Администратор · клас {FormatClassName(access.SchoolClass)}"
                : $"Класен ръководител на {FormatClassName(access.SchoolClass)}",
            StudentId = access.Student.Id,
            ClassId = access.SchoolClass.Id,
            StudentName = FormatPersonName(access.Student.User),
            StudentInitials = GetInitials(access.Student.User),
            ClassName = FormatClassName(access.SchoolClass),
            Feedbacks = MapStudentFeedbacks(
                    access.Student.Id,
                    subjectByClassSubjectId,
                    await feedbackRepository.GetAllAsync(cancellationToken),
                    manageableClassSubjectIds)
                .ToArray()
        };
    }

    private async Task<TeacherStudentAbsencesViewModel> BuildStudentAbsencesViewModelAsync(
        TeacherMainStudentAccess access,
        CancellationToken cancellationToken)
    {
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var subjectByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == access.Student.ClassId)
            .ToDictionary(classSubject => classSubject.Id, classSubject => classSubject.Subject.Name);

        return new TeacherStudentAbsencesViewModel
        {
            TeacherName = access.IsPrincipal ? "Директор" : FormatPersonName(access.Teacher!.User),
            TeacherInitials = access.IsPrincipal ? "Д" : GetInitials(access.Teacher!.User),
            MainMeta = access.IsPrincipal
                ? $"Администратор · клас {FormatClassName(access.SchoolClass)}"
                : $"Класен ръководител на {FormatClassName(access.SchoolClass)}",
            StudentId = access.Student.Id,
            ClassId = access.SchoolClass.Id,
            StudentName = FormatPersonName(access.Student.User),
            StudentInitials = GetInitials(access.Student.User),
            ClassName = FormatClassName(access.SchoolClass),
            Absences = MapStudentAbsences(
                    access.Student.Id,
                    subjectByClassSubjectId,
                    await absenceRepository.GetAllAsync(cancellationToken),
                    canExcuse: true)
                .ToArray()
        };
    }

    private static IEnumerable<TeacherStudentFeedbackDetailsItem> MapStudentFeedbacks(
        Guid studentId,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId,
        IReadOnlyList<Feedback> feedbacks,
        IReadOnlySet<Guid>? manageableClassSubjectIds = null)
    {
        return feedbacks
            .Where(feedback => feedback.StudentId == studentId)
            .OrderByDescending(feedback => feedback.Date)
            .ThenByDescending(feedback => feedback.CreatedAt)
            .Select(feedback => new TeacherStudentFeedbackDetailsItem
            {
                Id = feedback.Id,
                ClassSubjectId = feedback.ClassSubjectId,
                SubjectName = GetSubjectName(feedback.ClassSubjectId, subjectByClassSubjectId),
                Date = feedback.Date,
                TypeValue = (int)feedback.Type,
                TypeName = GetDisplayName(feedback.Type),
                Description = feedback.Description,
                CreatedAt = feedback.CreatedAt,
                CanManage = manageableClassSubjectIds?.Contains(feedback.ClassSubjectId) == true
            });
    }

    private static IEnumerable<TeacherStudentAbsenceDetailsItem> MapStudentAbsences(
        Guid studentId,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId,
        IReadOnlyList<Absence> absences,
        bool canExcuse)
    {
        return absences
            .Where(absence => absence.StudentId == studentId)
            .OrderByDescending(absence => absence.Date)
            .ThenByDescending(absence => absence.LessonNumber)
            .Select(absence => new TeacherStudentAbsenceDetailsItem
            {
                Id = absence.Id,
                ClassSubjectId = absence.ClassSubjectId,
                SubjectName = GetSubjectName(absence.ClassSubjectId, subjectByClassSubjectId),
                Date = absence.Date,
                LessonNumber = absence.LessonNumber,
                TypeValue = (int)absence.Type,
                TypeName = GetDisplayName(absence.Type),
                StatusValue = (int)absence.Status,
                StatusName = GetDisplayName(absence.Status),
                IsExcused = absence.Status == AbsenceStatus.Excused,
                ExcuseReasonValue = absence.ExcuseReason.HasValue ? (int)absence.ExcuseReason.Value : null,
                ExcuseReasonName = absence.ExcuseReason.HasValue ? GetDisplayName(absence.ExcuseReason.Value) : string.Empty,
                ExcuseNote = absence.ExcuseNote,
                CanExcuse = canExcuse
            });
    }

    private static HashSet<Guid> GetManageableClassSubjectIds(
        TeacherMainStudentAccess access,
        IEnumerable<ClassSubject> classSubjects)
    {
        if (access.IsPrincipal)
        {
            return classSubjects.Select(classSubject => classSubject.Id).ToHashSet();
        }

        return classSubjects
            .Where(classSubject => classSubject.TeacherId == access.Teacher!.Id)
            .Select(classSubject => classSubject.Id)
            .ToHashSet();
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

    private sealed record TeacherMainStudentAccess(Teacher? Teacher, Student Student, Class SchoolClass, bool IsPrincipal);

    private async Task<TeacherScheduleViewModel> BuildScheduleViewModelAsync(
        TeacherHomeViewModel homeViewModel,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var teacher = userId.HasValue
            ? (await teacherRepository.GetAllAsync(cancellationToken)).FirstOrDefault(currentTeacher => currentTeacher.UserId == userId.Value)
            : null;

        var entries = teacher is null
            ? Array.Empty<ClassScheduleEntry>()
            : (await classScheduleEntryRepository.GetAllAsync(cancellationToken))
                .Where(entry =>
                    entry.ClassSubject.TeacherId == teacher.Id ||
                    entry.SubstituteTeacherId == teacher.Id)
                .OrderBy(entry => entry.DayOfWeek)
                .ThenBy(entry => entry.PeriodNumber)
                .ToArray();

        return new TeacherScheduleViewModel
        {
            TeacherName = homeViewModel.TeacherName,
            TeacherInitials = homeViewModel.TeacherInitials,
            MainMeta = homeViewModel.MainMeta,
            Days = GetSchoolWeekDays()
                .Select(day => new TeacherScheduleDayViewModel
                {
                    DayOfWeek = day,
                    DayName = GetDayName(day),
                    Entries = entries
                        .Where(entry => entry.DayOfWeek == day)
                        .Select(entry => new TeacherScheduleEntryViewModel
                        {
                            Id = entry.Id,
                            ClassName = FormatClassName(entry.Class),
                            SubjectName = entry.ClassSubject.Subject.Name,
                            PeriodNumber = entry.PeriodNumber,
                            TimeRange = $"{entry.StartsAt:HH:mm} - {entry.EndsAt:HH:mm}",
                            IsSubstitution = entry.SubstituteTeacherId == teacher?.Id
                        })
                        .ToArray()
                })
                .ToArray()
        };
    }

    private async Task<TeacherCalendarViewModel> BuildCalendarViewModelAsync(
        TeacherHomeViewModel homeViewModel,
        int? year,
        int? month,
        CancellationToken cancellationToken)
    {
        var selectedMonth = GetSelectedMonth(year, month);
        var userId = GetCurrentUserId();
        var teacher = userId.HasValue
            ? (await teacherRepository.GetAllAsync(cancellationToken)).FirstOrDefault(currentTeacher => currentTeacher.UserId == userId.Value)
            : null;

        var relevantClassIds = new HashSet<Guid>();
        if (teacher is not null)
        {
            var classes = await classRepository.GetAllAsync(cancellationToken);
            var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);

            foreach (var classId in classSubjects
                         .Where(classSubject => classSubject.TeacherId == teacher.Id)
                         .Select(classSubject => classSubject.ClassId))
            {
                relevantClassIds.Add(classId);
            }

            foreach (var classId in classes
                         .Where(schoolClass => schoolClass.MainTeacherId == teacher.Id)
                         .Select(schoolClass => schoolClass.Id))
            {
                relevantClassIds.Add(classId);
            }
        }

        var events = teacher is null
            ? Array.Empty<Event>()
            : (await eventRepository.GetAllAsync(cancellationToken))
                .Where(schoolEvent =>
                    schoolEvent.Classes.Any(schoolClass => relevantClassIds.Contains(schoolClass.Id)) ||
                    (schoolEvent.ClassSubject?.TeacherId == teacher.Id))
                .OrderBy(schoolEvent => schoolEvent.Date)
                .ToArray();

        var monthStart = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
        var gridStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
        var days = Enumerable.Range(0, 42)
            .Select(offset =>
            {
                var date = gridStart.AddDays(offset);
                var dayEvents = events
                    .Where(schoolEvent => schoolEvent.Date.Date == date.Date)
                    .Select(MapCalendarEvent)
                    .ToArray();

                return new TeacherCalendarDayViewModel
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = date.Month == selectedMonth.Month,
                    IsToday = date.Date == DateTime.Today,
                    Events = dayEvents
                };
            })
            .ToArray();

        return new TeacherCalendarViewModel
        {
            TeacherName = homeViewModel.TeacherName,
            TeacherInitials = homeViewModel.TeacherInitials,
            MainMeta = homeViewModel.MainMeta,
            Year = selectedMonth.Year,
            Month = selectedMonth.Month,
            MonthName = CultureInfo.GetCultureInfo("bg-BG").DateTimeFormat.GetMonthName(selectedMonth.Month),
            CalendarDays = days,
            UpcomingEvents = events
                .Where(schoolEvent => schoolEvent.Date >= DateTime.Today)
                .Take(8)
                .Select(MapCalendarEvent)
                .ToArray()
        };
    }

    private static TeacherCalendarEventViewModel MapCalendarEvent(Event schoolEvent)
    {
        return new TeacherCalendarEventViewModel
        {
            Id = schoolEvent.Id,
            Title = schoolEvent.Title,
            EventTypeName = GetDisplayName(schoolEvent.EventType),
            EventTypeCssClass = GetEventTypeCssClass(schoolEvent.EventType),
            ClassSubjectName = schoolEvent.ClassSubject?.Subject.Name,
            ClassNames = string.Join(", ", schoolEvent.Classes.Select(FormatClassName).OrderBy(name => name)),
            Date = schoolEvent.Date,
            DayLabel = schoolEvent.Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("bg-BG")),
            TimeLabel = schoolEvent.Date.ToString("HH:mm", CultureInfo.GetCultureInfo("bg-BG"))
        };
    }

    private static DayOfWeek[] GetSchoolWeekDays()
    {
        return new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
    }

    private static DateTime GetSelectedMonth(int? year, int? month)
    {
        var now = DateTime.Today;
        var selectedYear = year.GetValueOrDefault(now.Year);
        var selectedMonth = month.GetValueOrDefault(now.Month);

        if (selectedYear < 2000 || selectedYear > 2100 || selectedMonth < 1 || selectedMonth > 12)
        {
            return new DateTime(now.Year, now.Month, 1);
        }

        return new DateTime(selectedYear, selectedMonth, 1);
    }

    private static string GetDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Понеделник",
            DayOfWeek.Tuesday => "Вторник",
            DayOfWeek.Wednesday => "Сряда",
            DayOfWeek.Thursday => "Четвъртък",
            DayOfWeek.Friday => "Петък",
            DayOfWeek.Saturday => "Събота",
            DayOfWeek.Sunday => "Неделя",
            _ => dayOfWeek.ToString()
        };
    }

    private static string FormatClassName(Class schoolClass)
    {
        return $"{schoolClass.GradeNumber}{schoolClass.Letter}";
    }

    private static string FormatPersonName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string GetInitials(User user)
    {
        var first = string.IsNullOrWhiteSpace(user.FirstName) ? string.Empty : user.FirstName[..1];
        var last = string.IsNullOrWhiteSpace(user.LastName) ? string.Empty : user.LastName[..1];
        return string.Concat(first, last).ToUpperInvariant();
    }

    private static string GetSubjectName(Guid classSubjectId, IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        return subjectByClassSubjectId.TryGetValue(classSubjectId, out var subjectName)
            ? subjectName
            : "Предмет";
    }

    private static int GetRoundedGrade(decimal value)
    {
        if (value < 3)
        {
            return 2;
        }

        return (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    private static string GetDisplayName(Enum value)
    {
        return value.GetType()
            .GetMember(value.ToString())
            .First()
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .OfType<DisplayAttribute>()
            .FirstOrDefault()
            ?.GetName() ?? value.ToString();
    }

    private static string GetEventTypeCssClass(EventType eventType)
    {
        return eventType switch
        {
            EventType.Test => "is-purple",
            EventType.Homework => "is-blue",
            EventType.Trip => "is-green",
            _ => "is-orange"
        };
    }
}

public class TeacherGradeRecordInputModel
{
    public Guid? ClassId { get; set; }

    public Guid StudentId { get; set; }

    public Guid ClassSubjectId { get; set; }

    public decimal Value { get; set; }

    public GradeType Type { get; set; }

    public string? Note { get; set; }
}

public class TeacherGradeEditInputModel
{
    public Guid GradeId { get; set; }

    public Guid StudentId { get; set; }

    public decimal Value { get; set; }

    public GradeType Type { get; set; }

    public string? Note { get; set; }
}

public class TeacherAbsenceRecordInputModel
{
    public Guid? ClassId { get; set; }

    public Guid StudentId { get; set; }

    public Guid ClassSubjectId { get; set; }

    public DateOnly Date { get; set; }

    public int LessonNumber { get; set; }

    public AbsenceType Type { get; set; }

    public AbsenceStatus Status { get; set; }

    public AbsenceExcuseReason? ExcuseReason { get; set; }

    public string? ExcuseNote { get; set; }
}

public class TeacherAbsenceEditInputModel
{
    public Guid AbsenceId { get; set; }

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

    public Guid ClassSubjectId { get; set; }

    public DateOnly Date { get; set; }

    public FeedbackType Type { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class TeacherFeedbackEditInputModel
{
    public Guid FeedbackId { get; set; }

    public Guid StudentId { get; set; }

    public DateOnly Date { get; set; }

    public FeedbackType Type { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class TeacherExcuseAbsenceInputModel
{
    public Guid AbsenceId { get; set; }

    public AbsenceExcuseReason ExcuseReason { get; set; } = AbsenceExcuseReason.Other;

    public string? ExcuseNote { get; set; }
}
