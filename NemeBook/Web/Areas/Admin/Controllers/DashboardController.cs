using Data;
using Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Principal")]
public class DashboardController : Controller
{
    private const int MaximumGradeBarHeight = 170;
    private const int MinimumGradeBarHeight = 18;

    private readonly NemeBookDbContext dbContext;

    public DashboardController(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await BuildPrincipalHomeViewModelAsync(cancellationToken);
        return View(viewModel);
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

    private sealed record PrincipalGradeRow(decimal Value, int GradeNumber, char Letter);
}
