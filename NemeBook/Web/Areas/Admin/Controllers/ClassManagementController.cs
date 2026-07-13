using Data;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Principal")]
public class ClassManagementController : Controller
{
    private readonly NemeBookDbContext dbContext;

    public ClassManagementController(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Students(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Class",
            "Клас",
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        var studentRows = await dbContext.Students
            .AsNoTracking()
            .Where(student => student.ClassId == classId && student.User.IsActive)
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.MiddleName)
            .ThenBy(student => student.User.LastName)
            .Select(student => new
            {
                student.Id,
                student.UserId,
                student.User.FirstName,
                student.User.MiddleName,
                student.User.LastName,
                AverageGrade = student.Grades
                    .Select(grade => (decimal?)grade.Value)
                    .Average(),
                PraiseCount = student.Feedbacks.Count(feedback => feedback.Type == FeedbackType.Praise),
                RemarkCount = student.Feedbacks.Count(feedback => feedback.Type == FeedbackType.Remark),
                AbsenceAndLatenessCount = student.Absences.Count(),
            })
            .ToListAsync(cancellationToken);

        viewModel.Students = studentRows
            .Select((student, index) => new PrincipalClassStudentViewModel
            {
                StudentId = student.Id,
                UserId = student.UserId,
                ClassNumber = index + 1,
                FullName = FormatFullName(student.FirstName, student.MiddleName, student.LastName),
                AverageGrade = student.AverageGrade.HasValue
                    ? Math.Round(student.AverageGrade.Value, 2)
                    : null,
                PraiseCount = student.PraiseCount,
                RemarkCount = student.RemarkCount,
                AbsenceAndLatenessCount = student.AbsenceAndLatenessCount,
            })
            .ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SearchStudentMatches(string? query, CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        if (searchTerms.Length == 0)
        {
            return Json(Array.Empty<object>());
        }

        var studentsQuery = dbContext.Students
            .AsNoTracking()
            .Where(student => student.User.IsActive);

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            studentsQuery = studentsQuery.Where(student =>
                student.User.FirstName.Contains(currentTerm) ||
                (student.User.MiddleName != null && student.User.MiddleName.Contains(currentTerm)) ||
                student.User.LastName.Contains(currentTerm));
        }

        var studentRows = await studentsQuery
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.MiddleName)
            .ThenBy(student => student.User.LastName)
            .Select(student => new
            {
                student.ClassId,
                student.User.FirstName,
                student.User.MiddleName,
                student.User.LastName,
                student.Class.GradeNumber,
                student.Class.Letter,
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        var results = studentRows.Select(student => new
        {
            fullName = FormatFullName(student.FirstName, student.MiddleName, student.LastName),
            className = $"{student.GradeNumber}{student.Letter}",
            url = Url.Action(nameof(Students), new { classId = student.ClassId }),
        });

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> SearchTeacherMatches(string? query, CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        if (searchTerms.Length == 0)
        {
            return Json(Array.Empty<object>());
        }

        var teachersQuery = dbContext.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.User.IsActive);

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            teachersQuery = teachersQuery.Where(teacher =>
                teacher.User.FirstName.Contains(currentTerm) ||
                (teacher.User.MiddleName != null && teacher.User.MiddleName.Contains(currentTerm)) ||
                teacher.User.LastName.Contains(currentTerm));
        }

        var teacherRows = await teachersQuery
            .OrderBy(teacher => teacher.User.FirstName)
            .ThenBy(teacher => teacher.User.MiddleName)
            .ThenBy(teacher => teacher.User.LastName)
            .Select(teacher => new
            {
                teacher.Id,
                teacher.User.FirstName,
                teacher.User.MiddleName,
                teacher.User.LastName,
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        var results = teacherRows.Select(teacher => new
        {
            id = teacher.Id,
            fullName = FormatFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName),
        });

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> SearchAvailableMainTeacherMatches(
        Guid classId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        var teachersQuery = dbContext.Teachers
            .AsNoTracking()
            .Where(teacher =>
                teacher.User.IsActive &&
                !dbContext.Classes.Any(schoolClass =>
                    schoolClass.Id != classId &&
                    schoolClass.MainTeacherId == teacher.Id));

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            teachersQuery = teachersQuery.Where(teacher =>
                teacher.User.FirstName.Contains(currentTerm) ||
                (teacher.User.MiddleName != null && teacher.User.MiddleName.Contains(currentTerm)) ||
                teacher.User.LastName.Contains(currentTerm));
        }

        var teacherRows = await teachersQuery
            .OrderBy(teacher => teacher.User.FirstName)
            .ThenBy(teacher => teacher.User.MiddleName)
            .ThenBy(teacher => teacher.User.LastName)
            .Select(teacher => new
            {
                teacher.Id,
                teacher.User.FirstName,
                teacher.User.MiddleName,
                teacher.User.LastName,
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        var results = teacherRows.Select(teacher => new
        {
            id = teacher.Id,
            fullName = FormatFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName),
        });

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> SearchClassSubjectTeacherMatches(
        Guid subjectId,
        string? query,
        bool includeAllTeachers = false,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        var teachersQuery = dbContext.Teachers
            .AsNoTracking()
            .Where(teacher =>
                teacher.User.IsActive &&
                (includeAllTeachers || teacher.TeacherSubjects.Any(teacherSubject => teacherSubject.SubjectId == subjectId)));

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            teachersQuery = teachersQuery.Where(teacher =>
                teacher.User.FirstName.Contains(currentTerm) ||
                (teacher.User.MiddleName != null && teacher.User.MiddleName.Contains(currentTerm)) ||
                teacher.User.LastName.Contains(currentTerm));
        }

        var teacherRows = await teachersQuery
            .OrderBy(teacher => teacher.User.FirstName)
            .ThenBy(teacher => teacher.User.MiddleName)
            .ThenBy(teacher => teacher.User.LastName)
            .Select(teacher => new
            {
                teacher.Id,
                teacher.User.FirstName,
                teacher.User.MiddleName,
                teacher.User.LastName,
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        var results = teacherRows.Select(teacher => new
        {
            id = teacher.Id,
            fullName = FormatFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName),
        });

        return Json(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMainTeacher(
        Guid classId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        var schoolClass = await dbContext.Classes
            .FirstOrDefaultAsync(currentClass => currentClass.Id == classId, cancellationToken);

        if (schoolClass is null)
        {
            return NotFound();
        }

        if (teacherId.HasValue)
        {
            var teacherExists = await dbContext.Teachers
                .AnyAsync(
                    teacher =>
                        teacher.Id == teacherId.Value &&
                        teacher.User.IsActive &&
                        !dbContext.Classes.Any(currentClass =>
                            currentClass.Id != classId &&
                            currentClass.MainTeacherId == teacher.Id),
                    cancellationToken);

            if (!teacherExists)
            {
                return RedirectToAction(nameof(Students), new { classId });
            }
        }

        schoolClass.MainTeacherId = teacherId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Students), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> Subjects(Guid classId, CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Subjects",
            "Предмети",
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        var classSubjectRows = await dbContext.ClassSubjects
            .AsNoTracking()
            .Where(classSubject => classSubject.ClassId == classId)
            .OrderBy(classSubject => classSubject.Subject.Name)
            .Select(classSubject => new
            {
                classSubject.Id,
                classSubject.SubjectId,
                SubjectName = classSubject.Subject.Name,
                classSubject.TeacherId,
                TeacherFirstName = classSubject.Teacher == null ? null : classSubject.Teacher.User.FirstName,
                TeacherMiddleName = classSubject.Teacher == null ? null : classSubject.Teacher.User.MiddleName,
                TeacherLastName = classSubject.Teacher == null ? null : classSubject.Teacher.User.LastName,
            })
            .ToListAsync(cancellationToken);

        viewModel.ClassSubjects = classSubjectRows
            .Select(classSubject => new PrincipalClassSubjectViewModel
            {
                ClassSubjectId = classSubject.Id,
                SubjectId = classSubject.SubjectId,
                SubjectName = classSubject.SubjectName,
                TeacherId = classSubject.TeacherId,
                TeacherName = classSubject.TeacherFirstName is null || classSubject.TeacherLastName is null
                    ? null
                    : FormatFullName(
                        classSubject.TeacherFirstName,
                        classSubject.TeacherMiddleName,
                        classSubject.TeacherLastName),
            })
            .ToList();

        var addedSubjectIds = viewModel.ClassSubjects
            .Select(classSubject => classSubject.SubjectId)
            .ToList();

        viewModel.SubjectOptions = await dbContext.Subjects
            .AsNoTracking()
            .Where(subject => !addedSubjectIds.Contains(subject.Id))
            .OrderBy(subject => subject.Name)
            .Select(subject => new PrincipalSubjectOptionViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
            })
            .ToListAsync(cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : name.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest();
        }

        var existingSubject = await dbContext.Subjects
            .FirstOrDefaultAsync(subject => subject.Name == normalizedName, cancellationToken);

        if (existingSubject is not null)
        {
            return Json(new
            {
                id = existingSubject.Id,
                name = existingSubject.Name,
            });
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
        };

        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Json(new
        {
            id = subject.Id,
            name = subject.Name,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClassSubject(
        Guid classId,
        Guid subjectId,
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        var classExists = await dbContext.Classes
            .AnyAsync(schoolClass => schoolClass.Id == classId, cancellationToken);

        if (!classExists)
        {
            return NotFound();
        }

        var subjectExists = await dbContext.Subjects
            .AnyAsync(subject => subject.Id == subjectId, cancellationToken);

        var teacherExists = await dbContext.Teachers
            .AnyAsync(
                teacher =>
                    teacher.Id == teacherId &&
                    teacher.User.IsActive,
                cancellationToken);

        if (!subjectExists || !teacherExists)
        {
            return RedirectToAction(nameof(Subjects), new { classId });
        }

        var existingClassSubject = await dbContext.ClassSubjects
            .FirstOrDefaultAsync(
                classSubject =>
                    classSubject.ClassId == classId &&
                    classSubject.SubjectId == subjectId,
                cancellationToken);

        if (existingClassSubject is null)
        {
            dbContext.ClassSubjects.Add(new ClassSubject
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                SubjectId = subjectId,
                TeacherId = teacherId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateClassSubjectTeacher(
        Guid classId,
        Guid classSubjectId,
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        var classSubject = await dbContext.ClassSubjects
            .FirstOrDefaultAsync(
                currentClassSubject =>
                    currentClassSubject.Id == classSubjectId &&
                    currentClassSubject.ClassId == classId,
                cancellationToken);

        if (classSubject is null)
        {
            return NotFound();
        }

        var teacherExists = await dbContext.Teachers
            .AnyAsync(
                teacher =>
                    teacher.Id == teacherId &&
                    teacher.User.IsActive,
                cancellationToken);

        if (teacherExists)
        {
            classSubject.TeacherId = teacherId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClassSubject(
        Guid classId,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var classSubject = await dbContext.ClassSubjects
            .FirstOrDefaultAsync(
                currentClassSubject =>
                    currentClassSubject.Id == classSubjectId &&
                    currentClassSubject.ClassId == classId,
                cancellationToken);

        if (classSubject is not null)
        {
            dbContext.ClassSubjects.Remove(classSubject);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Subjects), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(Guid classId, CancellationToken cancellationToken = default)
    {
        return await PlaceholderAsync(classId, "Schedule", "Програма", cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Events(Guid classId, CancellationToken cancellationToken = default)
    {
        return await PlaceholderAsync(classId, "Events", "Събития", cancellationToken);
    }

    private async Task<IActionResult> PlaceholderAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            activeTab,
            sectionTitle,
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        viewModel.EmptyMessage = "Тази секция ще бъде добавена по-късно.";
        return View("Placeholder", viewModel);
    }

    private async Task<PrincipalClassManagementViewModel?> BuildClassManagementViewModelAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.Classes
            .AsNoTracking()
            .Where(currentClass => currentClass.Id == classId)
            .Select(currentClass => new
            {
                currentClass.Id,
                currentClass.GradeNumber,
                currentClass.Letter,
                currentClass.MainTeacherId,
                MainTeacherIsActive = currentClass.MainTeacher != null && currentClass.MainTeacher.User.IsActive,
                MainTeacherFirstName = currentClass.MainTeacher == null ? null : currentClass.MainTeacher.User.FirstName,
                MainTeacherMiddleName = currentClass.MainTeacher == null ? null : currentClass.MainTeacher.User.MiddleName,
                MainTeacherLastName = currentClass.MainTeacher == null ? null : currentClass.MainTeacher.User.LastName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (schoolClass is null)
        {
            return null;
        }

        return new PrincipalClassManagementViewModel
        {
            ClassId = schoolClass.Id,
            ClassName = $"{schoolClass.GradeNumber}{schoolClass.Letter}",
            ActiveTab = activeTab,
            SectionTitle = sectionTitle,
            MainTeacherId = schoolClass.MainTeacherIsActive ? schoolClass.MainTeacherId : null,
            MainTeacherName = schoolClass.MainTeacherIsActive && schoolClass.MainTeacherId.HasValue
                ? FormatFullName(
                    schoolClass.MainTeacherFirstName ?? string.Empty,
                    schoolClass.MainTeacherMiddleName,
                    schoolClass.MainTeacherLastName ?? string.Empty)
                : null,
        };
    }

    private static string FormatFullName(string firstName, string? middleName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, middleName, lastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string[] GetSearchTerms(string? query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? Array.Empty<string>()
            : query.Trim().Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
