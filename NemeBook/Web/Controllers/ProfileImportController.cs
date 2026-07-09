using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.Registration;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize(Roles = "Principal")]
public class ProfileImportController : Controller
{
    private readonly IRegistrationImportParser importParser;
    private readonly IRegistrationService registrationService;
    private readonly ILogger<ProfileImportController> logger;

    public ProfileImportController(
        IRegistrationImportParser importParser,
        IRegistrationService registrationService,
        ILogger<ProfileImportController> logger)
    {
        this.importParser = importParser;
        this.registrationService = registrationService;
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ProfileImportUploadViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(
        ProfileImportUploadViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.StudentsFile is null && model.ParentsFile is null && model.TeachersFile is null)
        {
            ModelState.AddModelError(string.Empty, "Choose at least one Excel file to import.");
            return View("Index", model);
        }

        await ImportStudentsAsync(model, cancellationToken);
        await ImportTeachersAsync(model, cancellationToken);
        await ImportParentsAsync(model, cancellationToken);

        return View("Index", model);
    }

    private async Task ImportStudentsAsync(ProfileImportUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!IsProvided(model.StudentsFile))
        {
            return;
        }

        var result = new ProfileImportSectionResult
        {
            Title = "Students",
            FileName = model.StudentsFile!.FileName
        };

        try
        {
            await using var stream = model.StudentsFile.OpenReadStream();
            var students = await importParser.ParseStudentsAsync(stream, cancellationToken);
            result.Result = await registrationService.ImportStudentsAsync(students, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Student profile import failed.");
            result.ErrorMessage = ex.Message;
        }

        model.Results.Add(result);
    }

    private async Task ImportTeachersAsync(ProfileImportUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!IsProvided(model.TeachersFile))
        {
            return;
        }

        var result = new ProfileImportSectionResult
        {
            Title = "Teachers",
            FileName = model.TeachersFile!.FileName
        };

        try
        {
            await using var stream = model.TeachersFile.OpenReadStream();
            var teachers = await importParser.ParseTeachersAsync(stream, cancellationToken);
            result.Result = await registrationService.ImportTeachersAsync(teachers, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Teacher profile import failed.");
            result.ErrorMessage = ex.Message;
        }

        model.Results.Add(result);
    }

    private async Task ImportParentsAsync(ProfileImportUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!IsProvided(model.ParentsFile))
        {
            return;
        }

        var result = new ProfileImportSectionResult
        {
            Title = "Parents",
            FileName = model.ParentsFile!.FileName
        };

        try
        {
            await using var stream = model.ParentsFile.OpenReadStream();
            var parents = await importParser.ParseParentsAsync(stream, cancellationToken);
            result.Result = await registrationService.ImportParentsAsync(parents, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parent profile import failed.");
            result.ErrorMessage = ex.Message;
        }

        model.Results.Add(result);
    }

    private static bool IsProvided(IFormFile? file)
    {
        return file is not null && file.Length > 0;
    }
}
