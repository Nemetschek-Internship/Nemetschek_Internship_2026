using Entities.Enums;
using Entities.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Services.Repositories;
using Web.ViewModels;

namespace Web.Services.Admin;

public class PrincipalClassManagementService : IPrincipalClassManagementService
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

    private readonly IClassRepository classRepository;
    private readonly IClassScheduleEntryRepository scheduleEntryRepository;
    private readonly IClassSubjectRepository classSubjectRepository;
    private readonly IEventRepository eventRepository;
    private readonly IStudentRepository studentRepository;
    private readonly ISubjectRepository subjectRepository;
    private readonly ITeacherRepository teacherRepository;

    public PrincipalClassManagementService(
        IClassRepository classRepository,
        IClassScheduleEntryRepository scheduleEntryRepository,
        IClassSubjectRepository classSubjectRepository,
        IEventRepository eventRepository,
        IStudentRepository studentRepository,
        ISubjectRepository subjectRepository,
        ITeacherRepository teacherRepository)
    {
        this.classRepository = classRepository;
        this.scheduleEntryRepository = scheduleEntryRepository;
        this.classSubjectRepository = classSubjectRepository;
        this.eventRepository = eventRepository;
        this.studentRepository = studentRepository;
        this.subjectRepository = subjectRepository;
        this.teacherRepository = teacherRepository;
    }

    public async Task<PrincipalClassManagementViewModel?> BuildStudentsViewModelAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Class",
            "Клас",
            cancellationToken);

        if (viewModel is null)
        {
            return null;
        }

        var studentRows = (await studentRepository.GetAllAsync(cancellationToken))
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
            .ToList();

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

        return viewModel;
    }

    public async Task<IReadOnlyList<PrincipalStudentSearchResult>> SearchStudentMatchesAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);
        if (searchTerms.Length == 0)
        {
            return Array.Empty<PrincipalStudentSearchResult>();
        }

        var studentsQuery = (await studentRepository.GetAllAsync(cancellationToken))
            .Where(student => student.User.IsActive);

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            studentsQuery = studentsQuery.Where(student =>
                student.User.FirstName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase) ||
                (student.User.MiddleName != null &&
                 student.User.MiddleName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase)) ||
                student.User.LastName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase));
        }

        var studentRows = studentsQuery
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
            .ToList();

        return studentRows
            .Select(student => new PrincipalStudentSearchResult(
                student.ClassId,
                FormatFullName(student.FirstName, student.MiddleName, student.LastName),
                $"{student.GradeNumber}{student.Letter}"))
            .ToList();
    }

    public async Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchTeacherMatchesAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);
        if (searchTerms.Length == 0)
        {
            return Array.Empty<PrincipalTeacherSearchResult>();
        }

        var teachersQuery = (await teacherRepository.GetAllAsync(cancellationToken))
            .Where(teacher => teacher.User.IsActive);

        return await BuildTeacherSearchResultsAsync(teachersQuery, searchTerms, cancellationToken);
    }

    public async Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchAvailableMainTeacherMatchesAsync(
        Guid classId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        var assignedMainTeacherIds = (await classRepository.GetAllAsync(cancellationToken))
            .Where(schoolClass => schoolClass.Id != classId && schoolClass.MainTeacherId.HasValue)
            .Select(schoolClass => schoolClass.MainTeacherId!.Value)
            .ToHashSet();

        var teachersQuery = (await teacherRepository.GetAllAsync(cancellationToken))
            .Where(teacher =>
                teacher.User.IsActive &&
                !assignedMainTeacherIds.Contains(teacher.Id));

        return await BuildTeacherSearchResultsAsync(teachersQuery, searchTerms, cancellationToken);
    }

    public async Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchClassSubjectTeacherMatchesAsync(
        Guid subjectId,
        bool includeAllTeachers,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);

        var teachersQuery = (await teacherRepository.GetAllAsync(cancellationToken))
            .Where(teacher => teacher.User.IsActive);

        if (!includeAllTeachers)
        {
            teachersQuery = teachersQuery.Where(teacher =>
                teacher.TeacherSubjects.Any(teacherSubject => teacherSubject.SubjectId == subjectId));
        }

        return await BuildTeacherSearchResultsAsync(teachersQuery, searchTerms, cancellationToken);
    }

    public async Task AssignMainTeacherAsync(
        Guid classId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        var schoolClass = await classRepository.GetByIdAsync(classId, cancellationToken);

        if (schoolClass is null)
        {
            return;
        }

        if (!teacherId.HasValue)
        {
            schoolClass.MainTeacherId = null;
        }
        else
        {
            var assignedMainTeacherIds = (await classRepository.GetAllAsync(cancellationToken))
                .Where(currentClass => currentClass.Id != classId && currentClass.MainTeacherId.HasValue)
                .Select(currentClass => currentClass.MainTeacherId!.Value)
                .ToHashSet();

            var teacherExists = (await teacherRepository.GetAllAsync(cancellationToken))
                .Any(teacher =>
                    teacher.Id == teacherId.Value &&
                    teacher.User.IsActive &&
                    !assignedMainTeacherIds.Contains(teacher.Id));

            if (teacherExists)
            {
                schoolClass.MainTeacherId = teacherId.Value;
            }
        }

        await classRepository.UpdateAsync(schoolClass, cancellationToken);
    }

    public async Task<PrincipalClassManagementViewModel?> BuildSubjectsViewModelAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Subjects",
            "Предмети",
            cancellationToken);

        if (viewModel is null)
        {
            return null;
        }

        viewModel.ClassSubjects = await GetClassSubjectOptionsAsync(classId, cancellationToken);

        var assignedSubjectIds = viewModel.ClassSubjects
            .Select(classSubject => classSubject.SubjectId)
            .ToHashSet();

        viewModel.SubjectOptions = (await subjectRepository.GetAllAsync(cancellationToken))
            .Where(subject => !assignedSubjectIds.Contains(subject.Id))
            .OrderBy(subject => subject.Name)
            .Select(subject => new PrincipalSubjectOptionViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
            })
            .ToList();

        return viewModel;
    }

    public async Task<PrincipalSubjectOptionViewModel?> CreateSubjectAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var existingSubject = (await subjectRepository.GetAllAsync(cancellationToken))
            .Where(subject => subject.Name == normalizedName)
            .Select(subject => new PrincipalSubjectOptionViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
            })
            .FirstOrDefault();

        if (existingSubject is not null)
        {
            return existingSubject;
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
        };

        await subjectRepository.CreateAsync(subject, cancellationToken);

        return new PrincipalSubjectOptionViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
        };
    }

    public async Task AddClassSubjectAsync(
        Guid classId,
        Guid subjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        var classExists = await classRepository.GetByIdAsync(classId, cancellationToken) is not null;

        if (!classExists)
        {
            return;
        }

        var subjectExists = await subjectRepository.GetByIdAsync(subjectId, cancellationToken) is not null;

        var teacherExists = !teacherId.HasValue ||
                            (await teacherRepository.GetAllAsync(cancellationToken))
                            .Any(teacher => teacher.Id == teacherId.Value && teacher.User.IsActive);

        if (!subjectExists || !teacherExists)
        {
            return;
        }

        var existingClassSubject = (await classSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(classSubject =>
                    classSubject.ClassId == classId &&
                    classSubject.SubjectId == subjectId);

        if (existingClassSubject is null)
        {
            await classSubjectRepository.CreateAsync(new ClassSubject
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                SubjectId = subjectId,
                TeacherId = teacherId,
            }, cancellationToken);
        }
        else
        {
            existingClassSubject.TeacherId = teacherId;
            await classSubjectRepository.UpdateAsync(existingClassSubject, cancellationToken);
        }
    }

    public async Task UpdateClassSubjectTeacherAsync(
        Guid classId,
        Guid classSubjectId,
        Guid? teacherId,
        CancellationToken cancellationToken = default)
    {
        var classSubject = (await classSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(
                currentClassSubject =>
                    currentClassSubject.Id == classSubjectId &&
                    currentClassSubject.ClassId == classId);

        if (classSubject is null)
        {
            return;
        }

        var teacherExists = !teacherId.HasValue ||
                            (await teacherRepository.GetAllAsync(cancellationToken))
                            .Any(teacher =>
                                teacher.Id == teacherId.Value &&
                                teacher.User.IsActive);

        if (teacherExists)
        {
            classSubject.TeacherId = teacherId;
            await classSubjectRepository.UpdateAsync(classSubject, cancellationToken);
        }
    }

    public async Task DeleteClassSubjectAsync(
        Guid classId,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var classSubject = (await classSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(
                currentClassSubject =>
                    currentClassSubject.Id == classSubjectId &&
                    currentClassSubject.ClassId == classId);

        if (classSubject is not null)
        {
            await classSubjectRepository.DeleteAsync(classSubject.Id, cancellationToken);
        }
    }

    public async Task<PrincipalClassManagementViewModel?> BuildScheduleViewModelAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
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

        var scheduleRows = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .Where(scheduleEntry => scheduleEntry.ClassId == classId)
            .OrderBy(scheduleEntry => scheduleEntry.DayOfWeek)
            .ThenBy(scheduleEntry => scheduleEntry.PeriodNumber)
            .Select(scheduleEntry => new
            {
                scheduleEntry.Id,
                scheduleEntry.ClassSubjectId,
                TeacherId = scheduleEntry.ClassSubject.TeacherId,
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
            .ToList();

        viewModel.ScheduleDays = SchoolWeekDays
            .Select(dayOfWeek =>
            {
                var dayEntries = scheduleRows
                    .Where(scheduleEntry => scheduleEntry.DayOfWeek == dayOfWeek)
                    .Select(scheduleEntry => new PrincipalScheduleEntryViewModel
                    {
                        Id = scheduleEntry.Id,
                        ClassSubjectId = scheduleEntry.ClassSubjectId,
                        TeacherId = scheduleEntry.TeacherId,
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

        return viewModel;
    }

    public async Task<PrincipalScheduleMutationResult> AddScheduleEntryAsync(
        Guid classId,
        DayOfWeek dayOfWeek,
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var periodNumber = await GetNextSchedulePeriodNumberAsync(classId, dayOfWeek, cancellationToken);

        var classSubject = (await classSubjectRepository.GetAllAsync(cancellationToken))
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
            .FirstOrDefault();

        if (classSubject is null)
        {
            return new PrincipalScheduleMutationResult { NotFound = true };
        }

        var entryExists = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .Any(scheduleEntry =>
                    scheduleEntry.ClassId == classId &&
                    scheduleEntry.DayOfWeek == dayOfWeek &&
                    scheduleEntry.PeriodNumber == periodNumber);

        if (entryExists)
        {
            return new PrincipalScheduleMutationResult
            {
                Message = "За този ден и час вече има добавен предмет.",
            };
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
                return new PrincipalScheduleMutationResult { Conflict = conflict };
            }
        }

        var periodTimes = GetPeriodTimes(periodNumber);

        await scheduleEntryRepository.CreateAsync(new ClassScheduleEntry
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            ClassSubjectId = classSubject.Id,
            DayOfWeek = dayOfWeek,
            PeriodNumber = periodNumber,
            StartsAt = periodTimes.StartsAt,
            EndsAt = periodTimes.EndsAt,
        }, cancellationToken);

        return new PrincipalScheduleMutationResult();
    }

    public async Task<PrincipalScheduleMutationResult> UpdateScheduleEntryAsync(
        Guid classId,
        Guid scheduleEntryId,
        Guid classSubjectId,
        Guid? substituteTeacherId,
        CancellationToken cancellationToken = default)
    {
        var scheduleEntry = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(currentScheduleEntry =>
                    currentScheduleEntry.Id == scheduleEntryId &&
                    currentScheduleEntry.ClassId == classId);

        if (scheduleEntry is null)
        {
            return new PrincipalScheduleMutationResult { NotFound = true };
        }

        var classSubject = (await classSubjectRepository.GetAllAsync(cancellationToken))
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
            .FirstOrDefault();

        if (classSubject is null)
        {
            return new PrincipalScheduleMutationResult { NotFound = true };
        }

        if (substituteTeacherId.HasValue && classSubject.TeacherId == substituteTeacherId.Value)
        {
            return new PrincipalScheduleMutationResult
            {
                Message = "Избраният учител вече води този час и не може да бъде заместващ.",
            };
        }

        Guid? effectiveTeacherId = substituteTeacherId ?? classSubject.TeacherId;
        string? effectiveTeacherName = null;

        if (substituteTeacherId.HasValue)
        {
            var substituteTeacher = (await teacherRepository.GetAllAsync(cancellationToken))
                .Where(teacher => teacher.Id == substituteTeacherId.Value && teacher.User.IsActive)
                .Select(teacher => new
                {
                    teacher.Id,
                    teacher.User.FirstName,
                    teacher.User.MiddleName,
                    teacher.User.LastName,
                })
                .FirstOrDefault();

            if (substituteTeacher is null)
            {
                return new PrincipalScheduleMutationResult { NotFound = true };
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
                return new PrincipalScheduleMutationResult { Conflict = conflict };
            }
        }

        scheduleEntry.ClassSubjectId = classSubject.Id;
        scheduleEntry.SubstituteTeacherId = substituteTeacherId;

        await scheduleEntryRepository.UpdateAsync(scheduleEntry, cancellationToken);

        return new PrincipalScheduleMutationResult();
    }

    public async Task DeleteScheduleEntryAsync(
        Guid classId,
        Guid scheduleEntryId,
        CancellationToken cancellationToken = default)
    {
        var scheduleEntry = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(currentScheduleEntry =>
                    currentScheduleEntry.Id == scheduleEntryId &&
                    currentScheduleEntry.ClassId == classId);

        if (scheduleEntry is null)
        {
            return;
        }

        var deletedDayOfWeek = scheduleEntry.DayOfWeek;
        var deletedPeriodNumber = scheduleEntry.PeriodNumber;

        await scheduleEntryRepository.DeleteAsync(scheduleEntry.Id, cancellationToken);

        var entriesToShift = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .Where(currentScheduleEntry =>
                currentScheduleEntry.ClassId == classId &&
                currentScheduleEntry.DayOfWeek == deletedDayOfWeek &&
                currentScheduleEntry.PeriodNumber > deletedPeriodNumber)
            .OrderBy(currentScheduleEntry => currentScheduleEntry.PeriodNumber)
            .ToList();

        for (var index = 0; index < entriesToShift.Count; index++)
        {
            var newPeriodNumber = deletedPeriodNumber + index;
            var periodTimes = GetPeriodTimes(newPeriodNumber);

            entriesToShift[index].PeriodNumber = newPeriodNumber;
            entriesToShift[index].StartsAt = periodTimes.StartsAt;
            entriesToShift[index].EndsAt = periodTimes.EndsAt;
            await scheduleEntryRepository.UpdateAsync(entriesToShift[index], cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PrincipalTeacherSearchResult>> SearchFreeScheduleTeacherMatchesAsync(
        DayOfWeek dayOfWeek,
        int periodNumber,
        Guid? scheduleEntryId,
        Guid? classSubjectId,
        bool includeAllTeachers,
        Guid? excludedTeacherId,
        string? query,
        CancellationToken cancellationToken = default)
    {
        var busyTeacherIds = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .Where(scheduleEntry =>
                scheduleEntry.DayOfWeek == dayOfWeek &&
                scheduleEntry.PeriodNumber == periodNumber &&
                (!scheduleEntryId.HasValue || scheduleEntry.Id != scheduleEntryId.Value))
            .Select(scheduleEntry => scheduleEntry.SubstituteTeacherId ?? scheduleEntry.ClassSubject.TeacherId)
            .Where(teacherId => teacherId.HasValue)
            .Select(teacherId => teacherId!.Value)
            .Distinct()
            .ToList();

        var searchTerms = GetSearchTerms(query);
        var teachersQuery = (await teacherRepository.GetAllAsync(cancellationToken))
            .Where(teacher =>
                teacher.User.IsActive &&
                !busyTeacherIds.Contains(teacher.Id) &&
                (!excludedTeacherId.HasValue || teacher.Id != excludedTeacherId.Value));

        if (!includeAllTeachers)
        {
            if (!classSubjectId.HasValue)
            {
                return Array.Empty<PrincipalTeacherSearchResult>();
            }

            var subjectId = (await classSubjectRepository.GetAllAsync(cancellationToken))
                .Where(classSubject => classSubject.Id == classSubjectId.Value)
                .Select(classSubject => classSubject.SubjectId)
                .FirstOrDefault();

            if (subjectId == Guid.Empty)
            {
                return Array.Empty<PrincipalTeacherSearchResult>();
            }

            teachersQuery = teachersQuery.Where(teacher =>
                teacher.TeacherSubjects.Any(teacherSubject => teacherSubject.SubjectId == subjectId));
        }

        return await BuildTeacherSearchResultsAsync(teachersQuery, searchTerms, cancellationToken);
    }

    public async Task<PrincipalClassManagementViewModel?> BuildEventsViewModelAsync(
        Guid classId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            "Events",
            "Събития",
            cancellationToken);

        if (viewModel is null)
        {
            return null;
        }

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var calendarStart = monthStart.AddDays(-GetMondayBasedDayIndex(monthStart.DayOfWeek));
        var calendarEnd = monthEnd.AddDays((7 - GetMondayBasedDayIndex(monthEnd.DayOfWeek)) % 7);

        if ((calendarEnd - calendarStart).TotalDays < 35)
        {
            calendarEnd = calendarStart.AddDays(35);
        }

        viewModel.EventsYear = year;
        viewModel.EventsMonth = month;
        viewModel.EventsMonthName = CultureInfo
            .GetCultureInfo("bg-BG")
            .DateTimeFormat
            .GetMonthName(month);
        viewModel.ClassSubjects = await GetClassSubjectOptionsAsync(classId, cancellationToken);
        viewModel.EventTypeOptions = Enum.GetValues<EventType>()
            .Select(eventType => new PrincipalEventTypeOptionViewModel
            {
                Value = eventType.ToString(),
                Name = GetDisplayName(eventType),
            })
            .ToList();

        var calendarEventRows = await GetClassEventRowsAsync(
            classId,
            calendarStart,
            calendarEnd,
            cancellationToken);

        var calendarEvents = calendarEventRows
            .Select(MapClassEvent)
            .ToList();

        viewModel.CalendarDays = Enumerable
            .Range(0, (calendarEnd - calendarStart).Days)
            .Select(dayOffset =>
            {
                var date = calendarStart.AddDays(dayOffset);
                return new PrincipalCalendarDayViewModel
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsCurrentMonth = date.Month == month,
                    IsToday = date.Date == DateTime.Today,
                    Events = calendarEvents
                        .Where(schoolEvent => schoolEvent.Date.Date == date.Date)
                        .OrderBy(schoolEvent => schoolEvent.Date)
                        .ToList(),
                };
            })
            .ToList();

        var upcomingEventRows = await GetClassEventRowsAsync(
            classId,
            DateTime.Today,
            DateTime.Today.AddYears(1),
            cancellationToken,
            6);

        viewModel.UpcomingEvents = upcomingEventRows
            .Select(MapClassEvent)
            .ToList();

        return viewModel;
    }

    public async Task<PrincipalEventMutationResult> AddClassEventAsync(
        Guid classId,
        Guid createdByUserId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateEventInput(title, eventType, date, ref classSubjectId);
        if (validationMessage is not null)
        {
            return CreateEventMutationMessage(validationMessage, date);
        }

        var schoolClass = await classRepository.GetByIdAsync(classId, cancellationToken);

        if (schoolClass is null)
        {
            return new PrincipalEventMutationResult { NotFound = true };
        }

        var classSubjectMessage = await ValidateClassSubjectForEventAsync(
            classId,
            eventType,
            classSubjectId,
            cancellationToken);

        if (classSubjectMessage is not null)
        {
            return CreateEventMutationMessage(classSubjectMessage, date);
        }

        var schoolEvent = new Event
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            EventType = eventType,
            Date = date,
            ClassSubjectId = classSubjectId,
        };

        schoolEvent.Classes.Add(schoolClass);
        await eventRepository.CreateAsync(schoolEvent, cancellationToken);

        return CreateEventMutationRedirect(date);
    }

    public async Task<PrincipalEventMutationResult> UpdateClassEventAsync(
        Guid classId,
        Guid eventId,
        string title,
        string? description,
        EventType eventType,
        DateTime date,
        Guid? classSubjectId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateEventInput(title, eventType, date, ref classSubjectId);
        if (validationMessage is not null)
        {
            return new PrincipalEventMutationResult
            {
                Message = validationMessage,
                RedirectYear = returnYear,
                RedirectMonth = returnMonth,
            };
        }

        var schoolEvent = (await eventRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(currentEvent =>
                    currentEvent.Id == eventId &&
                    currentEvent.Classes.Any(schoolClass => schoolClass.Id == classId));

        if (schoolEvent is null)
        {
            return new PrincipalEventMutationResult { NotFound = true };
        }

        var classSubjectMessage = await ValidateClassSubjectForEventAsync(
            classId,
            eventType,
            classSubjectId,
            cancellationToken);

        if (classSubjectMessage is not null)
        {
            return new PrincipalEventMutationResult
            {
                Message = classSubjectMessage,
                RedirectYear = returnYear,
                RedirectMonth = returnMonth,
            };
        }

        schoolEvent.Title = title.Trim();
        schoolEvent.Description = description?.Trim() ?? string.Empty;
        schoolEvent.EventType = eventType;
        schoolEvent.Date = date;
        schoolEvent.ClassSubjectId = classSubjectId;

        await eventRepository.UpdateAsync(schoolEvent, cancellationToken);

        return CreateEventMutationRedirect(date);
    }

    public async Task DeleteClassEventAsync(
        Guid classId,
        Guid eventId,
        int returnYear,
        int returnMonth,
        CancellationToken cancellationToken = default)
    {
        var schoolEvent = (await eventRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(currentEvent =>
                    currentEvent.Id == eventId &&
                    currentEvent.Classes.Any(schoolClass => schoolClass.Id == classId));

        if (schoolEvent is not null)
        {
            if (schoolEvent.Classes.Count <= 1)
            {
                await eventRepository.DeleteAsync(schoolEvent.Id, cancellationToken);
            }
            else
            {
                var currentClass = schoolEvent.Classes.First(schoolClass => schoolClass.Id == classId);
                schoolEvent.Classes.Remove(currentClass);
                await eventRepository.UpdateAsync(schoolEvent, cancellationToken);
            }
        }
    }

    public async Task<PrincipalClassManagementViewModel?> BuildPlaceholderViewModelAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildClassManagementViewModelAsync(
            classId,
            activeTab,
            sectionTitle,
            cancellationToken);

        if (viewModel is null)
        {
            return null;
        }

        viewModel.EmptyMessage = "Тази секция ще бъде добавена по-късно.";
        return viewModel;
    }

    private async Task<PrincipalClassManagementViewModel?> BuildClassManagementViewModelAsync(
        Guid classId,
        string activeTab,
        string sectionTitle,
        CancellationToken cancellationToken)
    {
        var schoolClass = await classRepository.GetByIdAsync(classId, cancellationToken);

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
            MainTeacherName = schoolClass.MainTeacher?.User.FirstName is null || schoolClass.MainTeacher.User.LastName is null
                ? null
                : FormatFullName(
                    schoolClass.MainTeacher.User.FirstName,
                    schoolClass.MainTeacher.User.MiddleName,
                    schoolClass.MainTeacher.User.LastName),
        };
    }

    private async Task<List<PrincipalClassSubjectViewModel>> GetClassSubjectOptionsAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classSubjectRows = (await classSubjectRepository.GetAllAsync(cancellationToken))
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
            .ToList();

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

    private async Task<List<ClassEventRow>> GetClassEventRowsAsync(
        Guid classId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken,
        int? take = null)
    {
        var query = (await eventRepository.GetAllAsync(cancellationToken))
            .Where(schoolEvent =>
                schoolEvent.Classes.Any(schoolClass => schoolClass.Id == classId) &&
                schoolEvent.Date >= from &&
                schoolEvent.Date < to)
            .OrderBy(schoolEvent => schoolEvent.Date)
            .ThenBy(schoolEvent => schoolEvent.Title)
            .Select(schoolEvent => new ClassEventRow(
                schoolEvent.Id,
                schoolEvent.Title,
                schoolEvent.Description,
                schoolEvent.EventType,
                schoolEvent.Date,
                schoolEvent.ClassSubjectId,
                schoolEvent.ClassSubject == null ? null : schoolEvent.ClassSubject.Subject.Name));

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        return query.ToList();
    }

    private async Task<IReadOnlyList<PrincipalTeacherSearchResult>> BuildTeacherSearchResultsAsync(
        IEnumerable<Teacher> teachersQuery,
        string[] searchTerms,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            teachersQuery = teachersQuery.Where(teacher =>
                teacher.User.FirstName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase) ||
                (teacher.User.MiddleName != null &&
                 teacher.User.MiddleName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase)) ||
                teacher.User.LastName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase));
        }

        var teacherRows = teachersQuery
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
            .ToList();

        return teacherRows
            .Select(teacher => new PrincipalTeacherSearchResult(
                teacher.Id,
                FormatFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName)))
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
        var conflict = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
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
            .FirstOrDefault();

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

    private async Task<int> GetNextSchedulePeriodNumberAsync(
        Guid classId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        var usedPeriodNumbers = (await scheduleEntryRepository.GetAllAsync(cancellationToken))
            .Where(scheduleEntry =>
                scheduleEntry.ClassId == classId &&
                scheduleEntry.DayOfWeek == dayOfWeek)
            .Select(scheduleEntry => scheduleEntry.PeriodNumber)
            .ToList();

        return GetNextPeriodNumber(usedPeriodNumbers);
    }

    private async Task<string?> ValidateClassSubjectForEventAsync(
        Guid classId,
        EventType eventType,
        Guid? classSubjectId,
        CancellationToken cancellationToken)
    {
        if (eventType is not (EventType.Test or EventType.Homework) || !classSubjectId.HasValue)
        {
            return null;
        }

        var classSubjectExists = (await classSubjectRepository.GetAllAsync(cancellationToken))
            .Any(classSubject =>
                    classSubject.Id == classSubjectId.Value &&
                    classSubject.ClassId == classId);

        return classSubjectExists
            ? null
            : "Избраният предмет не е добавен към този клас.";
    }

    private static string? ValidateEventInput(
        string title,
        EventType eventType,
        DateTime date,
        ref Guid? classSubjectId)
    {
        if (string.IsNullOrWhiteSpace(title) ||
            date == default ||
            !Enum.IsDefined(eventType))
        {
            return "Попълнете заглавие, дата и тип на събитието.";
        }

        var eventNeedsSubject = eventType is EventType.Test or EventType.Homework;
        if (eventNeedsSubject && !classSubjectId.HasValue)
        {
            return "Изберете предмет за тест или домашна работа.";
        }

        if (!eventNeedsSubject)
        {
            classSubjectId = null;
        }

        return null;
    }

    private static PrincipalEventMutationResult CreateEventMutationRedirect(DateTime date)
    {
        return new PrincipalEventMutationResult
        {
            RedirectYear = date.Year,
            RedirectMonth = date.Month,
        };
    }

    private static PrincipalEventMutationResult CreateEventMutationMessage(string message, DateTime date)
    {
        var redirectDate = date == default ? DateTime.Today : date;

        return new PrincipalEventMutationResult
        {
            Message = message,
            RedirectYear = redirectDate.Year,
            RedirectMonth = redirectDate.Month,
        };
    }

    private static PrincipalClassEventViewModel MapClassEvent(ClassEventRow schoolEvent)
    {
        return new PrincipalClassEventViewModel
        {
            Id = schoolEvent.Id,
            Title = schoolEvent.Title,
            Description = string.IsNullOrWhiteSpace(schoolEvent.Description) ? null : schoolEvent.Description,
            EventTypeName = GetDisplayName(schoolEvent.EventType),
            EventTypeValue = schoolEvent.EventType.ToString(),
            EventTypeCssClass = GetEventTypeCssClass(schoolEvent.EventType),
            ClassSubjectId = schoolEvent.ClassSubjectId,
            ClassSubjectName = schoolEvent.ClassSubjectName,
            Date = schoolEvent.Date,
            EditDateValue = schoolEvent.Date.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            DayLabel = schoolEvent.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            TimeLabel = schoolEvent.Date.ToString("HH:mm", CultureInfo.InvariantCulture),
        };
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

    private static DateTime GetSelectedEventsMonth(int? year, int? month)
    {
        var today = DateTime.Today;

        if (!year.HasValue ||
            !month.HasValue ||
            year.Value < 1900 ||
            month.Value is < 1 or > 12)
        {
            return new DateTime(today.Year, today.Month, 1);
        }

        return new DateTime(year.Value, month.Value, 1);
    }

    private static int GetMondayBasedDayIndex(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;
    }

    private static string GetDisplayName(Enum value)
    {
        return value
            .GetType()
            .GetMember(value.ToString())[0]
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .Cast<DisplayAttribute>()
            .FirstOrDefault()
            ?.GetName() ?? value.ToString();
    }

    private static string GetEventTypeCssClass(EventType eventType)
    {
        return eventType switch
        {
            EventType.Test => "is-purple",
            EventType.Homework => "is-blue",
            EventType.Trip => "is-green",
            _ => "is-orange",
        };
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
            new[]
            {
                firstName,
                middleName,
                lastName,
            }.Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string[] GetSearchTerms(string? query)
    {
        return (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record ClassEventRow(
        Guid Id,
        string Title,
        string Description,
        EventType EventType,
        DateTime Date,
        Guid? ClassSubjectId,
        string? ClassSubjectName);
}
