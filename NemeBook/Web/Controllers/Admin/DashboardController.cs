using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Admin;

namespace Web.Controllers.Admin;

[Route("Admin/[controller]/[action]")]
[Authorize(Roles = "Principal")]
public class DashboardController : Controller
{
    private readonly IPrincipalDashboardService dashboardService;

    public DashboardController(IPrincipalDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("/Admin")]
    [HttpGet("/Admin/Dashboard")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await dashboardService.BuildHomeViewModelAsync(cancellationToken);
        return View(viewModel);
    }
}
