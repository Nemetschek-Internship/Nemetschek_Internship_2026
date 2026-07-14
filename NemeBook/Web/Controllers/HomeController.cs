using System.Diagnostics;
using System.Security.Claims;
using Entities.Enums;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly IAuthService authService;
    private readonly ILogger<HomeController> logger;

    public HomeController(IAuthService authService, ILogger<HomeController> logger)
    {
        this.authService = authService;
        this.logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var currentUser = await authService.GetUserByIdAsync(userId.Value, cancellationToken);
            if (currentUser?.Role == UserRole.Student)
            {
                return RedirectToAction("Index", "Student");
            }

            if (currentUser?.Role == UserRole.Teacher)
            {
                return RedirectToAction("Index", "Teacher");
            }
        }

        if (User.IsInRole("Principal"))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
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
