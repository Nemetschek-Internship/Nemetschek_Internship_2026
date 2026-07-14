using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services.Admin;

namespace Web.Controllers.Admin;

[Route("Admin/[controller]/[action]")]
[Authorize(Roles = "Principal")]
public class ReportsController : Controller
{
    private readonly IPrincipalReportsService reportsService;

    public ReportsController(IPrincipalReportsService reportsService)
    {
        this.reportsService = reportsService;
    }

    [HttpGet("/Admin/Reports")]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await reportsService.BuildReportAsync(
            reportType,
            fromDate,
            toDate,
            classId,
            studentId,
            cancellationToken);

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default)
    {
        var exportFile = await reportsService.ExportReportAsync(
            reportType,
            fromDate,
            toDate,
            classId,
            studentId,
            cancellationToken);

        return File(exportFile.Content, exportFile.ContentType, exportFile.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> SearchStudentMatches(
        string? query,
        Guid? classId,
        CancellationToken cancellationToken = default)
    {
        var matches = await reportsService.SearchStudentMatchesAsync(query, classId, cancellationToken);

        return Json(matches.Select(match => new
        {
            studentId = match.StudentId,
            fullName = match.FullName,
            className = match.ClassName,
        }));
    }
}
