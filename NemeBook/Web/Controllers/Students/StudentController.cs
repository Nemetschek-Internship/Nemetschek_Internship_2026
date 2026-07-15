using System.Security.Claims;
using Entities.Models;
using Entities.ViewModels.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.Students;
using Services.Repositories;

namespace Web.Controllers;

[Authorize]
public class StudentController : Controller
{
    private readonly IClassScheduleEntryRepository classScheduleEntryRepository;
    private readonly IStudentHomeService studentHomeService;
    private readonly IStudentRepository studentRepository;

    public StudentController(
        IStudentHomeService studentHomeService,
        IStudentRepository studentRepository,
        IClassScheduleEntryRepository classScheduleEntryRepository)
    {
        this.studentHomeService = studentHomeService;
        this.studentRepository = studentRepository;
        this.classScheduleEntryRepository = classScheduleEntryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await studentHomeService.GetHomeAsync(userId.Value, cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MyGrades(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await studentHomeService.GetHomeAsync(userId.Value, cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Calendar(int? year, int? month, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var viewModel = await studentHomeService.GetCalendarAsync(userId.Value, year, month, cancellationToken);
        if (viewModel is null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var students = await studentRepository.GetAllAsync(cancellationToken);
        var student = students.FirstOrDefault(currentStudent => currentStudent.UserId == userId.Value);
        if (student is null || student.User.IsDeleted || !student.User.IsActive)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var entries = (await classScheduleEntryRepository.GetAllAsync(cancellationToken))
            .Where(entry => entry.ClassId == student.ClassId)
            .OrderBy(entry => entry.DayOfWeek)
            .ThenBy(entry => entry.PeriodNumber)
            .ToArray();

        var viewModel = new TeacherScheduleViewModel
        {
            TeacherName = FormatPersonName(student.User),
            TeacherInitials = GetInitials(student.User),
            MainMeta = $"Ученик · клас {FormatClassName(student.Class)}",
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
                            TeacherName = FormatScheduleTeacherName(entry),
                            SubjectName = entry.ClassSubject.Subject.Name,
                            PeriodNumber = entry.PeriodNumber,
                            TimeRange = $"{entry.StartsAt:HH:mm} - {entry.EndsAt:HH:mm}",
                            IsSubstitution = entry.SubstituteTeacherId.HasValue
                        })
                        .ToArray()
                })
                .ToArray()
        };

        return View(viewModel);
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

    private static DayOfWeek[] GetSchoolWeekDays()
    {
        return new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
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

    private static string FormatScheduleTeacherName(ClassScheduleEntry entry)
    {
        var teacher = entry.SubstituteTeacher ?? entry.ClassSubject.Teacher;

        return teacher?.User is null
            ? "Няма назначен учител"
            : FormatPersonName(teacher.User);
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
}
