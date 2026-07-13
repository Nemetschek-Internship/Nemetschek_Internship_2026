using System.Security.Claims;
using Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Absences;
using Services.Interfaces.Absences;
using Services.Repositories;

namespace Web.Controllers;

/// <summary>
/// Auth setup, огледан от Web/Controllers/AccountController.cs:
///  - Cookie authentication (CookieAuthenticationDefaults.AuthenticationScheme).
///  - User.Id -> ClaimTypes.NameIdentifier, Role -> ClaimTypes.Role (user.Role.ToString()).
///  - GetCurrentUserId() следва nullable-return стила от AccountController, вместо exceptions.
///
/// ЗАБЕЛЕЖКА ЗА ДОПУСКАНЕ, което остава да провериш:
///  - Teacher.Id се намира чрез ITeacherRepository.GetAllAsync().FirstOrDefault(t => t.UserId == userId),
///    защото ITeacherRepository няма GetByUserIdAsync. Компромис заради наличния интерфейс.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AbsencesController : ControllerBase
{
    private readonly IAbsenceService _absenceService;
    private readonly ITeacherRepository _teacherRepository;

    public AbsencesController(IAbsenceService absenceService, ITeacherRepository teacherRepository)
    {
        _absenceService = absenceService;
        _teacherRepository = teacherRepository;
    }

    /// <summary>
    /// Маркира ученик за текущия учебен час.
    /// 1-ви клик -> закъснение, 2-ри клик (същия ученик/час/дата) -> неизвинено отсъствие.
    /// Достъпно само за учители.
    /// </summary>
    [HttpPost("mark")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Mark([FromBody] MarkAbsenceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var teacher = await GetCurrentTeacherAsync(userId.Value, cancellationToken);
        if (teacher is null)
            return Forbid();

        try
        {
            var absence = await _absenceService.MarkAsync(
                teacher.Id, request.StudentId, request.ClassScheduleEntryId, cancellationToken);

            return Ok(AbsenceDto.FromEntity(absence));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Извинява отсъствие/закъснение.
    /// Класният ръководител - само за своя клас. Директорът (Principal) - за всички класове.
    /// </summary>
    [HttpPut("{id:guid}/excuse")]
    [Authorize(Roles = "Teacher,Principal")]
    public async Task<IActionResult> Excuse(Guid id, [FromBody] ExcuseAbsenceRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        if (userId is null || role is null)
            return Unauthorized();

        var actingId = userId.Value;

        if (role == UserRole.Teacher)
        {
            var teacher = await GetCurrentTeacherAsync(userId.Value, cancellationToken);
            if (teacher is null)
                return Forbid();

            actingId = teacher.Id;
        }

        try
        {
            var absence = await _absenceService.ExcuseAsync(
                id, actingId, role.Value, request.ExcuseReason, request.ExcuseNote, cancellationToken);

            return Ok(AbsenceDto.FromEntity(absence));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Връща класовете/предметите, които преподава логнатият учител - за да избере от списък
    /// преди да въведе отсъствие/закъснение.
    /// </summary>
    [HttpGet("my-classes")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetMyClasses(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var teacher = await GetCurrentTeacherAsync(userId.Value, cancellationToken);
        if (teacher is null)
            return Forbid();

        var classSubjects = await _absenceService.GetTeacherClassSubjectsAsync(teacher.Id, cancellationToken);
        return Ok(classSubjects.Select(TeacherClassSubjectDto.FromEntity).ToList());
    }

    /// <summary>
    /// Намира текущия учебен час за избран ClassSubject, спрямо седмичната програма.
    /// Връща 204, ако в момента няма активен час за него.
    /// </summary>
    [HttpGet("current-lesson/{classSubjectId:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetCurrentLesson(Guid classSubjectId, CancellationToken cancellationToken)
    {
        var entry = await _absenceService.GetCurrentScheduleEntryAsync(classSubjectId, cancellationToken);
        if (entry is null)
            return NoContent();

        return Ok(CurrentLessonDto.FromEntity(entry));
    }

    /// <summary>
    /// Преглед на всички отсъствия/закъснения на конкретен ученик.
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var absences = await _absenceService.GetByStudentAsync(studentId, cancellationToken);
        return Ok(absences.Select(AbsenceDto.FromEntity).ToList());
    }

    /// <summary>
    /// Преглед на всички отсъствия/закъснения на учениците в конкретен клас.
    /// </summary>
    [HttpGet("class/{classId:guid}")]
    public async Task<IActionResult> GetByClass(Guid classId, CancellationToken cancellationToken)
    {
        var absences = await _absenceService.GetByClassAsync(classId, cancellationToken);
        return Ok(absences.Select(AbsenceDto.FromEntity).ToList());
    }

    /// <summary>
    /// Soft delete на отсъствие - само за администрация. Записът остава в базата (IsDeleted = true)
    /// за одит, но изчезва от справките.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Principal")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = GetCurrentUserRole();
        if (role is null)
            return Unauthorized();

        try
        {
            await _absenceService.DeleteAsync(id, role.Value, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ---------- helpers ----------
    // Стил, огледан от Web/Controllers/AccountController.cs (SignInUserAsync / GetCurrentUserId).

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    private UserRole? GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (roleClaim is not null && Enum.TryParse<UserRole>(roleClaim.Value, out var role))
        {
            return role;
        }

        return null;
    }

    private async Task<Entities.Models.Teacher?> GetCurrentTeacherAsync(Guid userId, CancellationToken cancellationToken)
    {
        var teachers = await _teacherRepository.GetAllAsync(cancellationToken);
        return teachers.FirstOrDefault(t => t.UserId == userId);
    }
}