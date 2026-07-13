using System.Diagnostics;
using System.Security.Claims;
using Entities.Enums;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IAuthService authService, ILogger<HomeController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            var currentUser = await _authService.GetUserByIdAsync(userId.Value, cancellationToken);
            if (currentUser?.Role == UserRole.Student)
            {
                return RedirectToAction("Index", "Student");
            }
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
