using Data;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.ViewModels;

namespace Web.Controllers.Admin;

[Route("Admin/[controller]/[action]")]
[Authorize(Roles = "Principal")]
public class ClassManagementController : Controller
{
    private static readonly DayOfWeek[] SchoolWeekDays =
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    private static readonly TimeOnly FirstPeriodStart = new TimeOnly(8, 0);
    private const int LessonMinutes = 40;
    private const int NormalBreakMinutes = 10;
    private const int LongBreakAfterPeriod = 5;
    private const int LongBreakMinutes = 30;

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
        var viewModel = await BuildScheduleViewModelAsync(classId, cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    private async Task<PrincipalClassManagementViewModel?> BuildScheduleViewModelAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Schedule",
            "Програма",
            cancellationToken);

        if (viewModel is null)
        {
            return null;
        }

        viewModel.ClassSubjects = await GetClassSubjectOptionsAsync(classId, cancellationToken);

        var scheduleRows = await dbContext.ClassScheduleEntries
            .AsNoTracking()
            .Where(scheduleEntry => scheduleEntry.ClassId == classId)
            .OrderBy(scheduleEntry => scheduleEntry.DayOfWeek)
            .ThenBy(scheduleEntry => scheduleEntry.PeriodNumber)
            .Select(scheduleEntry => new
            {
                scheduleEntry.Id,
                scheduleEntry.ClassSubjectId,
                scheduleEntry.SubstituteTeacherId,
                scheduleEntry.DayOfWeek,
                scheduleEntry.PeriodNumber,
                scheduleEntry.StartsAt,
                scheduleEntry.EndsAt,
                SubjectName = scheduleEntry.ClassSubject.Subject.Name,
                TeacherFirstName = scheduleEntry.ClassSubject.Teacher == null
                    ? null
                    : scheduleEntry.ClassSubject.Teacher.User.FirstName,
                TeacherMiddleName = scheduleEntry.ClassSubject.Teacher == null
                    ? null
                    : scheduleEntry.ClassSubject.Teacher.User.MiddleName,
                TeacherLastName = scheduleEntry.ClassSubject.Teacher == null
                    ? null
                    : scheduleEntry.ClassSubject.Teacher.User.LastName,
                SubstituteTeacherFirstName = scheduleEntry.SubstituteTeacher == null
                    ? null
                    : scheduleEntry.SubstituteTeacher.User.FirstName,
                SubstituteTeacherMiddleName = scheduleEntry.SubstituteTeacher == null
                    ? null
                    : scheduleEntry.SubstituteTeacher.User.MiddleName,
                SubstituteTeacherLastName = scheduleEntry.SubstituteTeacher == null
                    ? null
                    : scheduleEntry.SubstituteTeacher.User.LastName,
            })
            .ToListAsync(cancellationToken);

        viewModel.ScheduleDays = SchoolWeekDays
            .Select(dayOfWeek =>
            {
                var dayEntries = scheduleRows
                    .Where(scheduleEntry => scheduleEntry.DayOfWeek == dayOfWeek)
                    .Select(scheduleEntry => new PrincipalScheduleEntryViewModel
                    {
                        Id = scheduleEntry.Id,
                        ClassSubjectId = scheduleEntry.ClassSubjectId,
                        SubstituteTeacherId = scheduleEntry.SubstituteTeacherId,
                        DayOfWeek = scheduleEntry.DayOfWeek,
                        PeriodNumber = scheduleEntry.PeriodNumber,
                        SubjectName = scheduleEntry.SubjectName,
                        TeacherName = scheduleEntry.TeacherFirstName is null || scheduleEntry.TeacherLastName is null
                            ? null
                            : FormatFullName(
                                scheduleEntry.TeacherFirstName,
                                scheduleEntry.TeacherMiddleName,
                                scheduleEntry.TeacherLastName),
                        SubstituteTeacherName = scheduleEntry.SubstituteTeacherFirstName is null ||
                                                scheduleEntry.SubstituteTeacherLastName is null
                            ? null
                            : FormatFullName(
                                scheduleEntry.SubstituteTeacherFirstName,
                                scheduleEntry.SubstituteTeacherMiddleName,
                                scheduleEntry.SubstituteTeacherLastName),
                        TimeRange = FormatTimeRange(scheduleEntry.StartsAt, scheduleEntry.EndsAt),
                    })
                    .ToList();

                var nextPeriodNumber = GetNextPeriodNumber(dayEntries.Select(scheduleEntry => scheduleEntry.PeriodNumber));
                var nextPeriodTimes = GetPeriodTimes(nextPeriodNumber);

                return new PrincipalScheduleDayViewModel
                {
                    DayOfWeek = dayOfWeek,
                    DayName = GetBulgarianDayName(dayOfWeek),
                    Entries = dayEntries,
                    NextPeriodNumber = nextPeriodNumber,
                    NextPeriodTimeRange = FormatTimeRange(nextPeriodTimes.StartsAt, nextPeriodTimes.EndsAt),
                };
            })
            .ToList();

        viewModel.ScheduleConflict = BuildScheduleConflictFromTempData();

        return viewModel;
    }

    private async Task<IActionResult> ViewScheduleWithConflictAsync(
        Guid classId,
        PrincipalScheduleConflictViewModel conflict,
        CancellationToken cancellationToken)
    {
        var viewModel = await BuildScheduleViewModelAsync(classId, cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        viewModel.ScheduleConflict = conflict;
        return View("Schedule", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScheduleEntry(
        Guid classId,
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        periodNumber = await GetNextSchedulePeriodNumberAsync(classId, dayOfWeek, cancellationToken);

        var classSubject = await dbContext.ClassSubjects
            .AsNoTracking()
            .Where(currentClassSubject =>
                currentClassSubject.Id == classSubjectId &&
                currentClassSubject.ClassId == classId)
            .Select(currentClassSubject => new
            {
                currentClassSubject.Id,
                currentClassSubject.TeacherId,
                TeacherFirstName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.FirstName,
                TeacherMiddleName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.MiddleName,
                TeacherLastName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.LastName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (classSubject is null)
        {
            return NotFound();
        }

        var entryExists = await dbContext.ClassScheduleEntries
            .AnyAsync(scheduleEntry =>
                    scheduleEntry.ClassId == classId &&
                    scheduleEntry.DayOfWeek == dayOfWeek &&
                    scheduleEntry.PeriodNumber == periodNumber,
                cancellationToken);

        if (entryExists)
        {
            TempData["ScheduleMessage"] = "За този ден и час вече има добавен предмет.";
            return RedirectToAction(nameof(Schedule), new { classId });
        }

        if (classSubject.TeacherId.HasValue)
        {
            var teacherName = classSubject.TeacherFirstName is null || classSubject.TeacherLastName is null
                ? "Избраният учител"
                : FormatFullName(
                    classSubject.TeacherFirstName,
                    classSubject.TeacherMiddleName,
                    classSubject.TeacherLastName);

            var conflict = await FindTeacherScheduleConflictAsync(
                classSubject.TeacherId.Value,
                classId,
                null,
                dayOfWeek,
                periodNumber,
                teacherName,
                cancellationToken);

            if (conflict is not null)
            {
                return await ViewScheduleWithConflictAsync(classId, conflict, cancellationToken);
            }
        }

        var periodTimes = GetPeriodTimes(periodNumber);

        dbContext.ClassScheduleEntries.Add(new ClassScheduleEntry
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            ClassSubjectId = classSubject.Id,
            DayOfWeek = dayOfWeek,
            PeriodNumber = periodNumber,
            StartsAt = periodTimes.StartsAt,
            EndsAt = periodTimes.EndsAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Schedule), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScheduleEntry(
        Guid classId,
        Guid scheduleEntryId,
        Guid classSubjectId,
        Guid? substituteTeacherId,
        CancellationToken cancellationToken = default)
    {
        var scheduleEntry = await dbContext.ClassScheduleEntries
            .FirstOrDefaultAsync(currentScheduleEntry =>
                    currentScheduleEntry.Id == scheduleEntryId &&
                    currentScheduleEntry.ClassId == classId,
                cancellationToken);

        if (scheduleEntry is null)
        {
            return NotFound();
        }

        var classSubject = await dbContext.ClassSubjects
            .AsNoTracking()
            .Where(currentClassSubject =>
                currentClassSubject.Id == classSubjectId &&
                currentClassSubject.ClassId == classId)
            .Select(currentClassSubject => new
            {
                currentClassSubject.Id,
                currentClassSubject.TeacherId,
                TeacherFirstName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.FirstName,
                TeacherMiddleName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.MiddleName,
                TeacherLastName = currentClassSubject.Teacher == null ? null : currentClassSubject.Teacher.User.LastName,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (classSubject is null)
        {
            return RedirectToAction(nameof(Schedule), new { classId });
        }

        Guid? effectiveTeacherId = substituteTeacherId ?? classSubject.TeacherId;
        string? effectiveTeacherName = null;

        if (substituteTeacherId.HasValue)
        {
            var substituteTeacher = await dbContext.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.Id == substituteTeacherId.Value && teacher.User.IsActive)
                .Select(teacher => new
                {
                    teacher.Id,
                    teacher.User.FirstName,
                    teacher.User.MiddleName,
                    teacher.User.LastName,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (substituteTeacher is null)
            {
                return RedirectToAction(nameof(Schedule), new { classId });
            }

            effectiveTeacherName = FormatFullName(
                substituteTeacher.FirstName,
                substituteTeacher.MiddleName,
                substituteTeacher.LastName);
        }
        else if (classSubject.TeacherId.HasValue &&
                 classSubject.TeacherFirstName is not null &&
                 classSubject.TeacherLastName is not null)
        {
            effectiveTeacherName = FormatFullName(
                classSubject.TeacherFirstName,
                classSubject.TeacherMiddleName,
                classSubject.TeacherLastName);
        }

        if (effectiveTeacherId.HasValue)
        {
            var conflict = await FindTeacherScheduleConflictAsync(
                effectiveTeacherId.Value,
                classId,
                scheduleEntryId,
                scheduleEntry.DayOfWeek,
                scheduleEntry.PeriodNumber,
                effectiveTeacherName ?? "Избраният учител",
                cancellationToken);

            if (conflict is not null)
            {
                return await ViewScheduleWithConflictAsync(classId, conflict, cancellationToken);
            }
        }

        scheduleEntry.ClassSubjectId = classSubject.Id;
        scheduleEntry.SubstituteTeacherId = substituteTeacherId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Schedule), new { classId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScheduleEntry(
        Guid classId,
        Guid scheduleEntryId,
        CancellationToken cancellationToken = default)
    {
        var scheduleEntry = await dbContext.ClassScheduleEntries
            .FirstOrDefaultAsync(currentScheduleEntry =>
                    currentScheduleEntry.Id == scheduleEntryId &&
                    currentScheduleEntry.ClassId == classId,
                cancellationToken);

        if (scheduleEntry is not null)
        {
            var deletedDayOfWeek = scheduleEntry.DayOfWeek;
            var deletedPeriodNumber = scheduleEntry.PeriodNumber;

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            dbContext.ClassScheduleEntries.Remove(scheduleEntry);
            await dbContext.SaveChangesAsync(cancellationToken);

            var entriesToShift = await dbContext.ClassScheduleEntries
                .Where(currentScheduleEntry =>
                    currentScheduleEntry.ClassId == classId &&
                    currentScheduleEntry.DayOfWeek == deletedDayOfWeek &&
                    currentScheduleEntry.PeriodNumber > deletedPeriodNumber)
                .OrderBy(currentScheduleEntry => currentScheduleEntry.PeriodNumber)
                .ToListAsync(cancellationToken);

            for (var index = 0; index < entriesToShift.Count; index++)
            {
                var newPeriodNumber = deletedPeriodNumber + index;
                var periodTimes = GetPeriodTimes(newPeriodNumber);

                entriesToShift[index].PeriodNumber = newPeriodNumber;
                entriesToShift[index].StartsAt = periodTimes.StartsAt;
                entriesToShift[index].EndsAt = periodTimes.EndsAt;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Schedule), new { classId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchFreeScheduleTeacherMatches(
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid? scheduleEntryId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var busyTeacherIds = await dbContext.ClassScheduleEntries
            .AsNoTracking()
            .Where(scheduleEntry =>
                scheduleEntry.DayOfWeek == dayOfWeek &&
                scheduleEntry.PeriodNumber == periodNumber)
            .Select(scheduleEntry => scheduleEntry.SubstituteTeacherId ?? scheduleEntry.ClassSubject.TeacherId)
            .Where(teacherId => teacherId.HasValue)
            .Select(teacherId => teacherId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var searchTerms = GetSearchTerms(query);
        var teachersQuery = dbContext.Teachers
            .AsNoTracking()
            .Where(teacher =>
                teacher.User.IsActive &&
                !busyTeacherIds.Contains(teacher.Id));

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

        return Json(teacherRows.Select(teacher => new
        {
            id = teacher.Id,
            fullName = FormatFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName),
        }));
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
            MainTeacherId = schoolClass.MainTeacherId,
            MainTeacherName = schoolClass.MainTeacherId.HasValue
                ? FormatFullName(
                    schoolClass.MainTeacherFirstName ?? string.Empty,
                    schoolClass.MainTeacherMiddleName,
                    schoolClass.MainTeacherLastName ?? string.Empty)
                : null,
        };
    }

    private async Task<List<PrincipalClassSubjectViewModel>> GetClassSubjectOptionsAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
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

        return classSubjectRows
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
    }

    private async Task<PrincipalScheduleConflictViewModel?> FindTeacherScheduleConflictAsync(
        Guid teacherId,
        Guid currentClassId,
        Guid? currentScheduleEntryId,
        DayOfWeek dayOfWeek,
        int periodNumber,
        string teacherName,
        CancellationToken cancellationToken)
    {
        var conflict = await dbContext.ClassScheduleEntries
            .AsNoTracking()
            .Where(scheduleEntry =>
                scheduleEntry.DayOfWeek == dayOfWeek &&
                scheduleEntry.PeriodNumber == periodNumber &&
                scheduleEntry.ClassId != currentClassId &&
                (!currentScheduleEntryId.HasValue || scheduleEntry.Id != currentScheduleEntryId.Value) &&
                (scheduleEntry.SubstituteTeacherId == teacherId || scheduleEntry.ClassSubject.TeacherId == teacherId))
            .Select(scheduleEntry => new
            {
                scheduleEntry.ClassId,
                scheduleEntry.Class.GradeNumber,
                scheduleEntry.Class.Letter,
                scheduleEntry.StartsAt,
                scheduleEntry.EndsAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflict is null)
        {
            return null;
        }

        return new PrincipalScheduleConflictViewModel
        {
            TeacherName = teacherName,
            ClassId = conflict.ClassId,
            ClassName = $"{conflict.GradeNumber}{conflict.Letter}",
            DayName = GetBulgarianDayName(dayOfWeek),
            PeriodNumber = periodNumber,
            TimeRange = FormatTimeRange(conflict.StartsAt, conflict.EndsAt),
        };
    }

    private void StoreScheduleConflict(PrincipalScheduleConflictViewModel conflict)
    {
        TempData["ScheduleConflictTeacherName"] = conflict.TeacherName;
        TempData["ScheduleConflictClassName"] = conflict.ClassName;
        TempData["ScheduleConflictClassId"] = conflict.ClassId.ToString();
        TempData["ScheduleConflictDayName"] = conflict.DayName;
        TempData["ScheduleConflictPeriodNumber"] = conflict.PeriodNumber.ToString();
        TempData["ScheduleConflictTimeRange"] = conflict.TimeRange;
    }

    private PrincipalScheduleConflictViewModel? BuildScheduleConflictFromTempData()
    {
        var classIdValue = TempData["ScheduleConflictClassId"] as string;
        var periodNumberValue = TempData["ScheduleConflictPeriodNumber"] as string;

        if (!Guid.TryParse(classIdValue, out var classId) ||
            !int.TryParse(periodNumberValue, out var periodNumber))
        {
            return null;
        }

        return new PrincipalScheduleConflictViewModel
        {
            TeacherName = TempData["ScheduleConflictTeacherName"] as string ?? string.Empty,
            ClassName = TempData["ScheduleConflictClassName"] as string ?? string.Empty,
            ClassId = classId,
            DayName = TempData["ScheduleConflictDayName"] as string ?? string.Empty,
            PeriodNumber = periodNumber,
            TimeRange = TempData["ScheduleConflictTimeRange"] as string ?? string.Empty,
        };
    }

    private async Task<int> GetNextSchedulePeriodNumberAsync(
        Guid classId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        var usedPeriodNumbers = await dbContext.ClassScheduleEntries
            .AsNoTracking()
            .Where(scheduleEntry =>
                scheduleEntry.ClassId == classId &&
                scheduleEntry.DayOfWeek == dayOfWeek)
            .Select(scheduleEntry => scheduleEntry.PeriodNumber)
            .ToListAsync(cancellationToken);

        return GetNextPeriodNumber(usedPeriodNumbers);
    }

    private static int GetNextPeriodNumber(IEnumerable<int> usedPeriodNumbers)
    {
        var usedPeriods = usedPeriodNumbers
            .Where(periodNumber => periodNumber > 0)
            .ToHashSet();

        var periodNumber = 1;
        while (usedPeriods.Contains(periodNumber))
        {
            periodNumber++;
        }

        return periodNumber;
    }

    private static (TimeOnly StartsAt, TimeOnly EndsAt) GetPeriodTimes(int periodNumber)
    {
        var normalizedPeriodNumber = Math.Max(1, periodNumber);
        var previousPeriods = normalizedPeriodNumber - 1;
        var previousBreakMinutes = previousPeriods * NormalBreakMinutes;

        if (normalizedPeriodNumber > LongBreakAfterPeriod)
        {
            previousBreakMinutes += LongBreakMinutes - NormalBreakMinutes;
        }

        var startsAt = FirstPeriodStart.AddMinutes(previousPeriods * LessonMinutes + previousBreakMinutes);
        return (startsAt, startsAt.AddMinutes(LessonMinutes));
    }

    private static string FormatTimeRange(TimeOnly startsAt, TimeOnly endsAt)
    {
        return $"{startsAt:HH\\:mm} - {endsAt:HH\\:mm}";
    }

    private static string GetBulgarianDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Понеделник",
            DayOfWeek.Tuesday => "Вторник",
            DayOfWeek.Wednesday => "Сряда",
            DayOfWeek.Thursday => "Четвъртък",
            DayOfWeek.Friday => "Петък",
            DayOfWeek.Saturday => "Събота",
            DayOfWeek.Sunday => "Неделя",
            _ => dayOfWeek.ToString()
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
