using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Feedbacks;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Feedbacks;
using Services.Interfaces.Parents;
using Services.Interfaces.Students;
using Services.Interfaces.Teachers;

namespace Web.Controllers;

[Authorize]
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;
    private readonly IStudentService _studentService;
    private readonly IParentService _parentService;
    private readonly ITeacherService _teacherService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IFeedbackService feedbackService,
        IStudentService studentService,
        IParentService parentService,
        ITeacherService teacherService,
        IClassSubjectService classSubjectService,
        ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _studentService = studentService;
        _parentService = parentService;
        _teacherService = teacherService;
        _classSubjectService = classSubjectService;
        _logger = logger;
    }

    // Endpoint: въвеждане на забележка/похвала
    [HttpGet]
    [Authorize(Roles = "Teacher,Principal")]
    public async Task<IActionResult> Create(Guid? studentId = null)
    {
        await PopulateCreateViewBagAsync(studentId);
        return View(new CreateFeedbackRequest
        {
            StudentId = studentId ?? Guid.Empty
        });
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Principal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateViewBagAsync(request.StudentId);
            return View(request);
        }

        try
        {
            var id = await _feedbackService.CreateAsync(request, cancellationToken);
            _logger.LogInformation("Feedback {Id} created for student {StudentId}.", id, request.StudentId);
            TempData["SuccessMessage"] = "Забележката/похвалата е записана успешно.";
            return RedirectToAction(nameof(StudentFeedbacks), new { studentId = request.StudentId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCreateViewBagAsync(request.StudentId);
            return View(request);
        }
    }

    // Endpoint: преглед по ученик
    [HttpGet]
    public async Task<IActionResult> StudentFeedbacks(Guid studentId, CancellationToken cancellationToken)
    {
        if (!await CanAccessStudentAsync(studentId))
            return RedirectToAction("AccessDenied", "Account");

        var vm = await _feedbackService.GetByStudentAsync(studentId, cancellationToken);
        return View(vm);
    }

    // Endpoint: преглед по клас
    [HttpGet]
    [Authorize(Roles = "Teacher,Principal")]
    public async Task<IActionResult> ClassFeedbacks(Guid classId, CancellationToken cancellationToken)
    {
        var vm = await _feedbackService.GetByClassAsync(classId, cancellationToken);
        return View(vm);
    }

    private async Task PopulateCreateViewBagAsync(Guid? studentId)
    {
        var students = await _studentService.GetAllAsync();
        ViewBag.Students = students
            .Select(s => new { s.Id, Name = $"{s.User.FirstName} {s.User.LastName}" })
            .ToList();

        var classSubjects = await _classSubjectService.GetAllAsync();
        if (studentId.HasValue && studentId.Value != Guid.Empty)
        {
            var student = students.FirstOrDefault(s => s.Id == studentId.Value);
            if (student is not null)
            {
                classSubjects = classSubjects.Where(cs => cs.ClassId == student.ClassId).ToList();
            }
        }

        ViewBag.ClassSubjects = classSubjects
            .Select(cs => new { cs.Id, Name = cs.Subject?.Name ?? string.Empty })
            .ToList();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private async Task<bool> CanAccessStudentAsync(Guid studentId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return false;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Teacher" or "Principal") return true;

        if (role == "Student")
        {
            var students = await _studentService.GetAllAsync();
            var student = students.FirstOrDefault(s => s.UserId == userId.Value);
            return student is not null && student.Id == studentId;
        }

        if (role == "Parent")
        {
            var parents = await _parentService.GetAllAsync();
            var parent = parents.FirstOrDefault(p => p.UserId == userId.Value);
            return parent is not null && parent.Students.Any(s => s.Id == studentId);
        }

        return false;
    }
}
