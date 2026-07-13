using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Principal")]
public class ClassesController : Controller
{
    private readonly NemeBookDbContext dbContext;

    public ClassesController(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
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
}
