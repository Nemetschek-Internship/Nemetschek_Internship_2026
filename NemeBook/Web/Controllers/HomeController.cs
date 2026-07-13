using System.Diagnostics;
using System.Security.Claims;
using Data;
using Entities.Enums;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Web.ViewModels;

namespace Web.Controllers;

public class HomeController : Controller
{
    private const int MaximumGradeBarHeight = 170;
    private const int MinimumGradeBarHeight = 18;

    private readonly IAuthService _authService;
    private readonly ILogger<HomeController> _logger;
    private readonly NemeBookDbContext dbContext;

    public HomeController(IAuthService authService, ILogger<HomeController> logger, NemeBookDbContext dbContext)
    {
        _authService = authService;
        _logger = logger;
        this.dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var currentUser = await _authService.GetUserByIdAsync(userId.Value, cancellationToken);
            if (currentUser?.Role == UserRole.Student)
            {
                return RedirectToAction("Index", "Student");
            }
        }

        var viewModel = User.IsInRole("Principal")
            ? await BuildPrincipalHomeViewModelAsync(cancellationToken)
            : new PrincipalHomeViewModel();

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<PrincipalHomeViewModel> BuildPrincipalHomeViewModelAsync(CancellationToken cancellationToken)
    {
        var viewModel = new PrincipalHomeViewModel
        {
            Reports = CreateDefaultReports(),
        };

        var gradeRows = await dbContext.Grades
            .AsNoTracking()
            .Select(grade => new PrincipalGradeRow(
                grade.Value,
                grade.Student.Class.GradeNumber,
                grade.Student.Class.Letter))
            .ToListAsync(cancellationToken);

        if (gradeRows.Count > 0)
        {
            var gradeValues = gradeRows.Select(grade => grade.Value).ToList();
            viewModel.SchoolAverageGrade = Math.Round(gradeValues.Average(), 2);
            viewModel.Reports[0].Value = viewModel.SchoolAverageGrade.Value.ToString("0.00");

            var groupedGrades = gradeValues
                .GroupBy(value => Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 2, 6))
                .ToDictionary(group => group.Key, group => group.Count());

            var maximumCount = groupedGrades.Values.DefaultIfEmpty(0).Max();

            viewModel.GradeDistribution = Enumerable.Range(2, 5)
                .Select(grade =>
                {
                    groupedGrades.TryGetValue(grade, out var count);

                    return new PrincipalGradeDistributionViewModel
                    {
                        Grade = grade,
                        Count = count,
                        BarHeight = CalculateBarHeight(count, maximumCount),
                    };
                })
                .ToList();

            viewModel.ClassesNeedingAttention = gradeRows
                .GroupBy(grade => new
                {
                    grade.GradeNumber,
                    grade.Letter,
                })
                .Select(group => new PrincipalClassAttentionViewModel
                {
                    ClassName = $"{group.Key.GradeNumber}{group.Key.Letter}",
                    AverageGrade = Math.Round(group.Average(grade => grade.Value), 2),
                })
                .OrderBy(schoolClass => schoolClass.AverageGrade)
                .ThenBy(schoolClass => schoolClass.ClassName)
                .Take(3)
                .ToList();
        }

        var absencesCount = await dbContext.Absences
            .AsNoTracking()
            .CountAsync(cancellationToken);

        if (absencesCount > 0)
        {
            viewModel.Reports[1].Value = absencesCount.ToString();
        }

        var unexcusedAbsencesCount = await dbContext.Absences
            .AsNoTracking()
            .CountAsync(absence => absence.Status == AbsenceStatus.Unexcused, cancellationToken);

        if (unexcusedAbsencesCount > 0)
        {
            viewModel.Reports[2].Value = unexcusedAbsencesCount.ToString();
        }

        var feedbacksCount = await dbContext.Feedbacks
            .AsNoTracking()
            .CountAsync(cancellationToken);

        if (feedbacksCount > 0)
        {
            viewModel.Reports[3].Value = feedbacksCount.ToString();
        }

        return viewModel;
    }

    private static List<PrincipalReportItemViewModel> CreateDefaultReports()
    {
        return new List<PrincipalReportItemViewModel>
        {
            new PrincipalReportItemViewModel
            {
                Label = "Академично представяне",
                CssClass = "is-purple",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Посещаемост",
                CssClass = "is-blue",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Дисциплина",
                CssClass = "is-green",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Участие",
                CssClass = "is-orange",
            },
        };
    }

    private static int CalculateBarHeight(int count, int maximumCount)
    {
        if (count == 0 || maximumCount == 0)
        {
            return 0;
        }

        return MinimumGradeBarHeight + (int)Math.Round(
            (decimal)count / maximumCount * (MaximumGradeBarHeight - MinimumGradeBarHeight));
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

    private sealed record PrincipalGradeRow(decimal Value, int GradeNumber, char Letter);
}
