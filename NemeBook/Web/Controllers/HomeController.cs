using System.Diagnostics;
using Entities.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("Principal"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
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
}
