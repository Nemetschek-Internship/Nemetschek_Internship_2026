using Entities.Enums;
using Services.Repositories;
using Web.ViewModels;

namespace Web.Services.Admin;

public class PrincipalDashboardService : IPrincipalDashboardService
{
    private const int MaximumGradeBarHeight = 170;
    private const int MinimumGradeBarHeight = 18;

    private readonly IAbsenceRepository absenceRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IGradeRepository gradeRepository;

    public PrincipalDashboardService(
        IAbsenceRepository absenceRepository,
        IFeedbackRepository feedbackRepository,
        IGradeRepository gradeRepository)
    {
        this.absenceRepository = absenceRepository;
        this.feedbackRepository = feedbackRepository;
        this.gradeRepository = gradeRepository;
    }

    public async Task<PrincipalHomeViewModel> BuildHomeViewModelAsync(CancellationToken cancellationToken = default)
    {
        var viewModel = new PrincipalHomeViewModel
        {
            Reports = CreateDefaultReports(),
        };

        var gradeRows = (await gradeRepository.GetAllAsync(cancellationToken))
            .Select(grade => new PrincipalGradeRow(
                grade.Value,
                grade.Student.Class.GradeNumber,
                grade.Student.Class.Letter))
            .ToList();

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
                .Where(schoolClass => schoolClass.AverageGrade < 4.00m)
                .OrderBy(schoolClass => schoolClass.AverageGrade)
                .ThenBy(schoolClass => schoolClass.ClassName)
                .Take(3)
                .ToList();
        }

        var absences = await absenceRepository.GetAllAsync(cancellationToken);
        var absencesCount = absences.Count;

        if (absencesCount > 0)
        {
            viewModel.Reports[1].Value = absencesCount.ToString();
        }

        var unexcusedAbsencesCount = absences.Count(absence => absence.Status == AbsenceStatus.Unexcused);

        if (unexcusedAbsencesCount > 0)
        {
            viewModel.Reports[2].Value = unexcusedAbsencesCount.ToString();
        }

        var feedbacksCount = (await feedbackRepository.GetAllAsync(cancellationToken)).Count;

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
                ReportType = PrincipalReportTypes.Academic,
                CssClass = "is-purple",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Посещаемост",
                ReportType = PrincipalReportTypes.Absences,
                CssClass = "is-blue",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Дисциплина",
                ReportType = PrincipalReportTypes.UnexcusedAbsences,
                CssClass = "is-green",
            },
            new PrincipalReportItemViewModel
            {
                Label = "Участие",
                ReportType = PrincipalReportTypes.Feedback,
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
