using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Students;
using Services.Interfaces.Classes;
using Services.Interfaces.Students;
using Services.Repositories;

namespace Services.Services.Students;

public class StudentHomeService : IStudentHomeService
{
    private readonly IStudentRepository studentRepository;
    private readonly IClassService classService;
    private readonly IGradeRepository gradeRepository;
    private readonly IClassSubjectRepository classSubjectRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IAbsenceRepository absenceRepository;
    private readonly IClassScheduleEntryRepository scheduleEntryRepository;
    private readonly IEventRepository eventRepository;
    private readonly IAccountsRepository accountsRepository;

    public StudentHomeService(
        IStudentRepository studentRepository,
        IClassService classService,
        IGradeRepository gradeRepository,
        IClassSubjectRepository classSubjectRepository,
        IFeedbackRepository feedbackRepository,
        IAbsenceRepository absenceRepository,
        IClassScheduleEntryRepository scheduleEntryRepository,
        IEventRepository eventRepository,
        IAccountsRepository accountsRepository)
    {
        this.studentRepository = studentRepository;
        this.classService = classService;
        this.gradeRepository = gradeRepository;
        this.classSubjectRepository = classSubjectRepository;
        this.feedbackRepository = feedbackRepository;
        this.absenceRepository = absenceRepository;
        this.scheduleEntryRepository = scheduleEntryRepository;
        this.eventRepository = eventRepository;
        this.accountsRepository = accountsRepository;
    }

    public async Task<StudentHomeViewModel?> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var students = await studentRepository.GetAllAsync(cancellationToken);
        var student = students.FirstOrDefault(existingStudent => existingStudent.UserId == userId);
        if (student is null)
        {
            var user = await accountsRepository.GetByIdAsync(userId, cancellationToken);
            return user?.Role == UserRole.Student
                ? CreateSeededStudentHome(user)
                : null;
        }

        var classEntity = await classService.GetByIdAsync(student.ClassId, cancellationToken);
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var subjectByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == student.ClassId)
            .ToDictionary(
                classSubject => classSubject.Id,
                classSubject => classSubject.Subject.Name);
        var teacherByClassSubjectId = classSubjects
            .Where(classSubject => classSubject.ClassId == student.ClassId)
            .ToDictionary(
                classSubject => classSubject.Id,
                classSubject => FormatTeacherName(classSubject.Teacher));

        var grades = await gradeRepository.GetGradesByStudentIdAsync(student.Id, cancellationToken: cancellationToken);
        var feedbacks = await feedbackRepository.GetAllAsync(cancellationToken);
        var absences = await absenceRepository.GetAllAsync(cancellationToken);
        var scheduleEntries = await scheduleEntryRepository.GetAllAsync(cancellationToken);

        var studentFeedbacks = feedbacks
            .Where(feedback => feedback.StudentId == student.Id)
            .OrderByDescending(feedback => feedback.Date)
            .ThenByDescending(feedback => feedback.CreatedAt)
            .ToList();

        var studentAbsences = absences
            .Where(absence => absence.StudentId == student.Id)
            .OrderByDescending(absence => absence.Date)
            .ThenByDescending(absence => absence.CreatedAt)
            .ToList();

        var academicSubjects = BuildAcademicSubjects(grades, subjectByClassSubjectId, teacherByClassSubjectId);
        var feedbackDetails = BuildFeedbackDetails(studentFeedbacks, subjectByClassSubjectId, teacherByClassSubjectId);
        var absenceDetails = BuildAbsenceDetails(studentAbsences, subjectByClassSubjectId, teacherByClassSubjectId);

        return new StudentHomeViewModel
        {
            StudentName = FormatStudentName(student),
            StudentInitials = FormatInitials(student.User.FirstName, student.User.LastName),
            ClassName = classEntity is null ? string.Empty : $"{classEntity.GradeNumber}{classEntity.Letter}",
            GradeCount = grades.Count,
            OverallAverage = grades.Any() ? Math.Round(grades.Average(grade => grade.Value), 2) : 0,
            SubjectProgress = BuildSubjectProgress(grades, subjectByClassSubjectId),
            AcademicSubjects = academicSubjects,
            SubjectRecords = BuildSubjectRecords(academicSubjects, absenceDetails, feedbackDetails),
            RecentGrades = BuildRecentGrades(grades, subjectByClassSubjectId),
            Feedbacks = feedbackDetails,
            FeedbackSummary = BuildFeedbackSummary(studentFeedbacks, subjectByClassSubjectId),
            Absences = absenceDetails,
            AbsenceSummary = BuildAbsenceSummary(studentAbsences, subjectByClassSubjectId),
            TodaysSchedule = BuildTodaysSchedule(scheduleEntries, student.ClassId)
        };
    }

    public async Task<StudentCalendarViewModel?> GetCalendarAsync(
        Guid userId,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var students = await studentRepository.GetAllAsync(cancellationToken);
        var student = students.FirstOrDefault(existingStudent => existingStudent.UserId == userId);
        if (student is null)
        {
            return null;
        }

        var classEntity = await classService.GetByIdAsync(student.ClassId, cancellationToken);
        if (classEntity is null)
        {
            return null;
        }

        var selectedMonth = GetSelectedMonth(year, month);
        var monthStart = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var calendarStart = monthStart.AddDays(-GetMondayBasedDayIndex(monthStart.DayOfWeek));
        var calendarEnd = monthEnd.AddDays((7 - GetMondayBasedDayIndex(monthEnd.DayOfWeek)) % 7);

        if ((calendarEnd - calendarStart).TotalDays < 35)
        {
            calendarEnd = calendarStart.AddDays(35);
        }

        var classEvents = (await eventRepository.GetAllAsync(cancellationToken))
            .Where(schoolEvent =>
                schoolEvent.Date >= calendarStart &&
                schoolEvent.Date < calendarEnd &&
                (schoolEvent.Classes.Any(schoolClass => schoolClass.Id == student.ClassId) ||
                 schoolEvent.ClassSubject?.ClassId == student.ClassId))
            .OrderBy(schoolEvent => schoolEvent.Date)
            .Select(MapCalendarEvent)
            .ToList();

        return new StudentCalendarViewModel
        {
            StudentName = FormatStudentName(student),
            StudentInitials = FormatInitials(student.User.FirstName, student.User.LastName),
            ClassName = $"{classEntity.GradeNumber}{classEntity.Letter}",
            Year = selectedMonth.Year,
            Month = selectedMonth.Month,
            MonthName = CultureInfo.GetCultureInfo("bg-BG").DateTimeFormat.GetMonthName(selectedMonth.Month),
            CalendarDays = Enumerable
                .Range(0, (calendarEnd - calendarStart).Days)
                .Select(offset =>
                {
                    var date = calendarStart.AddDays(offset);
                    return new StudentCalendarDayViewModel
                    {
                        Date = date,
                        DayNumber = date.Day,
                        IsCurrentMonth = date.Month == selectedMonth.Month,
                        IsToday = date.Date == DateTime.Today,
                        Events = classEvents
                            .Where(schoolEvent => schoolEvent.Date.Date == date.Date)
                            .ToList()
                    };
                })
                .ToList(),
            UpcomingEvents = classEvents
                .Where(schoolEvent => schoolEvent.Date >= DateTime.Today)
                .Take(8)
                .ToList()
        };
    }

    private static StudentHomeViewModel CreateSeededStudentHome(User user)
    {
        return new StudentHomeViewModel
        {
            StudentName = FormatUserName(user),
            StudentInitials = FormatInitials(user.FirstName, user.LastName),
            ClassName = "Няма зададен клас",
            TodaysSchedule = Array.Empty<StudentScheduleItem>(),
            FeedbackSummary = new StudentSummaryCard
            {
                Count = 0,
                Label = "отзива",
                Detail = "Няма скорошни отзиви"
            },
            AbsenceSummary = new StudentSummaryCard
            {
                Count = 0,
                Label = "отсъствия",
                Detail = "Няма въведени отсъствия"
            }
        };
    }

    private static IReadOnlyList<StudentSubjectProgressItem> BuildSubjectProgress(
        IReadOnlyList<Grade> grades,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        var accents = new[] { "is-purple", "is-blue", "is-green" };

        var gradesBySubject = grades
            .GroupBy(grade => grade.ClassSubjectId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return subjectByClassSubjectId
            .Select(subject =>
            {
                var subjectGrades = gradesBySubject.GetValueOrDefault(subject.Key, new List<Grade>());
                var average = subjectGrades.Any()
                    ? Math.Round(subjectGrades.Average(grade => grade.Value), 2)
                    : 0;

                return new
                {
                    SubjectName = subject.Value,
                    Average = average,
                    GradeCount = subjectGrades.Count,
                    ExcellentGradeCount = subjectGrades.Count(grade => grade.Value == 6)
                };
            })
            .OrderByDescending(subject => subject.ExcellentGradeCount)
            .ThenByDescending(subject => subject.Average)
            .ThenBy(subject => subject.SubjectName)
            .Select((subject, index) => new StudentSubjectProgressItem
            {
                SubjectName = subject.SubjectName,
                Average = subject.Average,
                GradeCount = subject.GradeCount,
                ExcellentGradeCount = subject.ExcellentGradeCount,
                ProgressPercent = ConvertGradeAverageToPercent(subject.Average),
                AccentClass = accents[index % accents.Length]
            })
            .ToList();
    }

    private static IReadOnlyList<StudentAcademicSubjectItem> BuildAcademicSubjects(
        IReadOnlyList<Grade> grades,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId,
        IReadOnlyDictionary<Guid, string> teacherByClassSubjectId)
    {
        var gradesBySubject = grades
            .GroupBy(grade => grade.ClassSubjectId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(grade => grade.CreatedAt).ToList());

        return subjectByClassSubjectId
            .OrderBy(subject => subject.Value)
            .Select(subject =>
            {
                var subjectGrades = gradesBySubject.GetValueOrDefault(subject.Key, new List<Grade>());

                return new StudentAcademicSubjectItem
                {
                    SubjectName = subject.Value,
                    Average = subjectGrades.Any()
                        ? Math.Round(subjectGrades.Average(grade => grade.Value), 2)
                        : 0,
                    GradeCount = subjectGrades.Count,
                    Grades = subjectGrades
                        .Select(grade => new StudentGradeDetailItem
                        {
                            Id = grade.Id,
                            SubjectName = subject.Value,
                            Value = grade.Value,
                            Type = GetDisplayName(grade.Type),
                            TeacherName = GetTeacherName(grade.ClassSubjectId, teacherByClassSubjectId),
                            Date = grade.CreatedAt,
                            Comment = string.IsNullOrWhiteSpace(grade.Note)
                                ? "Няма коментар"
                                : grade.Note
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    private static IReadOnlyList<StudentSubjectRecordsItem> BuildSubjectRecords(
        IReadOnlyList<StudentAcademicSubjectItem> academicSubjects,
        IReadOnlyList<StudentAbsenceDetailItem> absences,
        IReadOnlyList<StudentFeedbackDetailItem> feedbacks)
    {
        return academicSubjects
            .Select(subject => new StudentSubjectRecordsItem
            {
                SubjectName = subject.SubjectName,
                Average = subject.Average,
                GradeCount = subject.GradeCount,
                Grades = subject.Grades,
                Absences = absences
                    .Where(absence => absence.SubjectName == subject.SubjectName)
                    .ToList(),
                Feedbacks = feedbacks
                    .Where(feedback => feedback.SubjectName == subject.SubjectName)
                    .ToList()
            })
            .ToList();
    }

    private static IReadOnlyList<StudentTimelineItem> BuildRecentGrades(
        IReadOnlyList<Grade> grades,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        return grades
            .OrderByDescending(grade => grade.CreatedAt)
            .Take(3)
            .Select(grade => new StudentTimelineItem
            {
                Title = GetSubjectName(grade.ClassSubjectId, subjectByClassSubjectId),
                Detail = $"{GetDisplayName(grade.Type)} - {grade.CreatedAt:dd.MM.yyyy}",
                Value = FormatDisplayedGrade(grade.Value)
            })
            .ToList();
    }

    private static string FormatDisplayedGrade(decimal value)
    {
        if (value < 3)
        {
            return "2";
        }

        return Math.Clamp((int)Math.Floor(value + 0.5m), 3, 6).ToString();
    }

    private static IReadOnlyList<StudentFeedbackDetailItem> BuildFeedbackDetails(
        IReadOnlyList<Feedback> feedbacks,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId,
        IReadOnlyDictionary<Guid, string> teacherByClassSubjectId)
    {
        return feedbacks
            .Select(feedback => new StudentFeedbackDetailItem
            {
                SubjectName = GetSubjectName(feedback.ClassSubjectId, subjectByClassSubjectId),
                Type = GetDisplayName(feedback.Type),
                TeacherName = GetTeacherName(feedback.ClassSubjectId, teacherByClassSubjectId),
                Date = feedback.Date,
                Comment = string.IsNullOrWhiteSpace(feedback.Description)
                    ? "Няма коментар"
                    : feedback.Description
            })
            .ToList();
    }

    private static StudentSummaryCard BuildFeedbackSummary(
        IReadOnlyList<Feedback> feedbacks,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        var latestFeedback = feedbacks.FirstOrDefault();

        return new StudentSummaryCard
        {
            Count = feedbacks.Count,
            Label = feedbacks.Count == 1 ? "отзив" : "отзива",
            Detail = latestFeedback is null
                ? "Няма скорошни отзиви"
                : $"{GetSubjectName(latestFeedback.ClassSubjectId, subjectByClassSubjectId)} - {latestFeedback.Date:dd.MM}"
            };
    }

    private static IReadOnlyList<StudentAbsenceDetailItem> BuildAbsenceDetails(
        IReadOnlyList<Absence> absences,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId,
        IReadOnlyDictionary<Guid, string> teacherByClassSubjectId)
    {
        return absences
            .Select(absence => new StudentAbsenceDetailItem
            {
                SubjectName = GetSubjectName(absence.ClassSubjectId, subjectByClassSubjectId),
                Type = GetDisplayName(absence.Type),
                Status = GetDisplayName(absence.Status),
                TeacherName = GetTeacherName(absence.ClassSubjectId, teacherByClassSubjectId),
                Date = absence.Date,
                LessonNumber = absence.LessonNumber,
                ExcuseReason = absence.ExcuseReason.HasValue
                    ? GetDisplayName(absence.ExcuseReason.Value)
                    : "Няма причина",
                Comment = string.IsNullOrWhiteSpace(absence.ExcuseNote)
                    ? "Няма коментар"
                    : absence.ExcuseNote
            })
            .ToList();
    }

    private static StudentSummaryCard BuildAbsenceSummary(
        IReadOnlyList<Absence> absences,
        IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        var latestAbsence = absences.FirstOrDefault();

        return new StudentSummaryCard
        {
            Count = absences.Count,
            Label = absences.Count == 1 ? "отсъствие" : "отсъствия",
            Detail = latestAbsence is null
                ? "Няма въведени отсъствия"
                : $"{GetSubjectName(latestAbsence.ClassSubjectId, subjectByClassSubjectId)} - {latestAbsence.Date:dd.MM}"
        };
    }

    private static IReadOnlyList<StudentScheduleItem> BuildTodaysSchedule(
        IReadOnlyList<ClassScheduleEntry> scheduleEntries,
        Guid classId)
    {
        var todaySchedule = scheduleEntries
            .Where(scheduleEntry =>
                scheduleEntry.ClassId == classId &&
                scheduleEntry.DayOfWeek == DateTime.Today.DayOfWeek)
            .OrderBy(scheduleEntry => scheduleEntry.PeriodNumber)
            .Select(scheduleEntry => new StudentScheduleItem
            {
                PeriodNumber = scheduleEntry.PeriodNumber,
                SubjectName = scheduleEntry.ClassSubject.Subject.Name,
                TimeRange = $"{scheduleEntry.StartsAt:HH:mm} - {scheduleEntry.EndsAt:HH:mm}"
            })
            .ToList();

        return todaySchedule.Count > 0
            ? todaySchedule
            : Array.Empty<StudentScheduleItem>();
    }

    private static string FormatStudentName(Student student)
    {
        return FormatUserName(student.User);
    }

    private static string FormatUserName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string FormatInitials(string firstName, string lastName)
    {
        var firstInitial = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName[0].ToString();
        var lastInitial = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName[0].ToString();

        return $"{firstInitial}{lastInitial}".ToUpperInvariant();
    }

    private static string GetSubjectName(Guid classSubjectId, IReadOnlyDictionary<Guid, string> subjectByClassSubjectId)
    {
        return subjectByClassSubjectId.GetValueOrDefault(classSubjectId, "Предмет");
    }

    private static string GetTeacherName(Guid classSubjectId, IReadOnlyDictionary<Guid, string> teacherByClassSubjectId)
    {
        return teacherByClassSubjectId.GetValueOrDefault(classSubjectId, "Няма зададен учител");
    }

    private static string FormatTeacherName(Teacher? teacher)
    {
        return teacher?.User is null
            ? "Няма зададен учител"
            : FormatUserName(teacher.User);
    }

    private static string GetDisplayName(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?
            .GetCustomAttributes(typeof(DisplayAttribute), false)
            .OfType<DisplayAttribute>()
            .FirstOrDefault()?
            .Name ?? value.ToString();
    }

    private static int ConvertGradeAverageToPercent(decimal average)
    {
        if (average <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(average / 6 * 100), 0, 100);
    }

    private static DateTime GetSelectedMonth(int? year, int? month)
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
        return ((int)dayOfWeek + 6) % 7;
    }

    private static StudentCalendarEventViewModel MapCalendarEvent(Event schoolEvent)
    {
        return new StudentCalendarEventViewModel
        {
            Id = schoolEvent.Id,
            Title = schoolEvent.Title,
            Description = schoolEvent.Description,
            EventTypeName = GetDisplayName(schoolEvent.EventType),
            EventTypeCssClass = GetEventTypeCssClass(schoolEvent.EventType),
            ClassSubjectName = schoolEvent.ClassSubject?.Subject.Name,
            Date = schoolEvent.Date,
            DayLabel = schoolEvent.Date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("bg-BG")),
            TimeLabel = schoolEvent.Date.ToString("HH:mm", CultureInfo.GetCultureInfo("bg-BG"))
        };
    }

    private static string GetEventTypeCssClass(EventType eventType)
    {
        return eventType switch
        {
            EventType.Test => "is-purple",
            EventType.Homework => "is-blue",
            EventType.Trip => "is-green",
            _ => "is-orange"
        };
    }
}
