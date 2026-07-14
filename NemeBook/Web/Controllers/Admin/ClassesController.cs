using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Admin;

namespace Web.Controllers.Admin;

[Route("Admin/[controller]/[action]")]
[Authorize(Roles = "Principal")]
public class ClassesController : Controller
{
    private readonly IPrincipalClassesService classesService;

    public ClassesController(IPrincipalClassesService classesService)
    {
        this.classesService = classesService;
    }

    [HttpGet]
    public async Task<IActionResult> AllClasses(int grade = 1, CancellationToken cancellationToken = default)
    {
        var viewModel = await classesService.BuildClassesViewModelAsync(grade, cancellationToken);
        return View(viewModel);
    }
}
