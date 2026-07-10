using Data;
using Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize(Roles = "Principal")]
public class ClassManagementController : Controller
{
    private readonly NemeBookDbContext dbContext;

    public ClassManagementController(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Students(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Students",
            "Ученици",
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        var studentRows = await dbContext.Students
            .AsNoTracking()
            .Where(student => student.ClassId == classId)
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.MiddleName)
            .ThenBy(student => student.User.LastName)
            .Select(student => new
            {
                student.Id,
                student.User.FirstName,
                student.User.MiddleName,
                student.User.LastName,
                AverageGrade = student.Grades
                    .Select(grade => (decimal?)grade.Value)
                    .Average(),
                PraiseCount = student.Feedbacks.Count(feedback => feedback.Type == FeedbackType.Praise),
                RemarkCount = student.Feedbacks.Count(feedback => feedback.Type == FeedbackType.Remark),
                AbsenceAndLatenessCount = student.Absences.Count(),
            })
            .ToListAsync(cancellationToken);

        viewModel.Students = studentRows
            .Select((student, index) => new PrincipalClassStudentViewModel
            {
                StudentId = student.Id,
                ClassNumber = index + 1,
                FullName = FormatFullName(student.FirstName, student.MiddleName, student.LastName),
                AverageGrade = student.AverageGrade.HasValue
                    ? Math.Round(student.AverageGrade.Value, 2)
                    : null,
                PraiseCount = student.PraiseCount,
                RemarkCount = student.RemarkCount,
                AbsenceAndLatenessCount = student.AbsenceAndLatenessCount,
            })
            .ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Subjects(Guid classId, CancellationToken cancellationToken = default)
    {
        return await PlaceholderAsync(classId, "Subjects", "Предмети", cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(Guid classId, CancellationToken cancellationToken = default)
    {
        return await PlaceholderAsync(classId, "Schedule", "Програма", cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Events(Guid classId, CancellationToken cancellationToken = default)
    {
        return await PlaceholderAsync(classId, "Events", "Събития", cancellationToken);
    }

    private async Task<IActionResult> PlaceholderAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            activeTab,
            sectionTitle,
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        viewModel.EmptyMessage = "Тази секция ще бъде добавена по-късно.";
        return View("Placeholder", viewModel);
    }

    private async Task<PrincipalClassManagementViewModel?> BuildClassManagementViewModelAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .Where(currentClass => currentClass.Id == classId)
            .Select(currentClass => new
            {
                currentClass.Id,
                currentClass.GradeNumber,
                currentClass.Letter,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (schoolClass is null)
        {
            return null;
        }

        return new PrincipalClassManagementViewModel
        {
            ClassId = schoolClass.Id,
            ClassName = $"{schoolClass.GradeNumber}{schoolClass.Letter}",
            ActiveTab = activeTab,
            SectionTitle = sectionTitle,
        };
    }

    private static string FormatFullName(string firstName, string? middleName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, middleName, lastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)));
    }
}
