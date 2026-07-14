using Services.Repositories;
using Web.ViewModels;

namespace Web.Services.Admin;

public class PrincipalClassesService : IPrincipalClassesService
{
    private readonly IClassRepository classRepository;
    private readonly IGradeRepository gradeRepository;

    public PrincipalClassesService(
        IClassRepository classRepository,
        IGradeRepository gradeRepository)
    {
        this.classRepository = classRepository;
        this.gradeRepository = gradeRepository;
    }

    public async Task<PrincipalClassesViewModel> BuildClassesViewModelAsync(
        int grade,
        CancellationToken cancellationToken = default)
    {
        var selectedGrade = Math.Clamp(grade, 1, 7);

        var classRows = (await classRepository.GetAllAsync(cancellationToken))
            .Where(schoolClass => schoolClass.GradeNumber == selectedGrade)
            .OrderBy(schoolClass => schoolClass.Letter)
            .Select(schoolClass => new
            {
                schoolClass.Id,
                schoolClass.GradeNumber,
                schoolClass.Letter,
            })
            .ToList();

        var classIds = classRows.Select(schoolClass => schoolClass.Id).ToList();

        var gradeRows = (await gradeRepository.GetAllAsync(cancellationToken))
            .Where(gradeEntry => classIds.Contains(gradeEntry.Student.ClassId))
            .Select(gradeEntry => new
            {
                gradeEntry.Student.ClassId,
                gradeEntry.Value,
            })
            .ToList();

        var classAverages = gradeRows
            .GroupBy(gradeEntry => gradeEntry.ClassId)
            .ToDictionary(
                group => group.Key,
                group => Math.Round(group.Average(gradeEntry => gradeEntry.Value), 2));

        return new PrincipalClassesViewModel
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
    }
}
