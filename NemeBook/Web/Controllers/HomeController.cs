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

    public HomeController(IAuthService authService)
    {
        this.authService = authService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var currentUser = await authService.GetUserByIdAsync(userId.Value, cancellationToken);
        return currentUser?.Role switch
        {
            UserRole.Student => RedirectToAction("Index", "Student"),
            UserRole.Teacher => RedirectToAction("Index", "Teacher"),
            UserRole.Parent => RedirectToAction("Parent", "Chat"),
            UserRole.Principal => RedirectToAction("Index", "Dashboard"),
            _ => RedirectToAction("Login", "Account")
        };
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
