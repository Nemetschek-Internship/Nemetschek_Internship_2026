using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.Students;

namespace Web.Controllers;

[Authorize]
public class StudentController : Controller
{
    private readonly IStudentHomeService studentHomeService;

    public StudentController(IStudentHomeService studentHomeService)
    {
        this.studentHomeService = studentHomeService;
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

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
