namespace Web.ViewModels;

public class PrincipalClassManagementViewModel
{
    public Guid ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string ActiveTab { get; set; } = "Class";

    public string SectionTitle { get; set; } = "Клас";

    public string? EmptyMessage { get; set; }

    public Guid? MainTeacherId { get; set; }

    public string? MainTeacherName { get; set; }

    public List<PrincipalTeacherOptionViewModel> AvailableMainTeachers { get; set; } = new List<PrincipalTeacherOptionViewModel>();

    public List<PrincipalClassSubjectViewModel> ClassSubjects { get; set; } = new List<PrincipalClassSubjectViewModel>();

    public List<PrincipalScheduleDayViewModel> ScheduleDays { get; set; } = new List<PrincipalScheduleDayViewModel>();

    public PrincipalScheduleConflictViewModel? ScheduleConflict { get; set; }

    public int EventsYear { get; set; }

    public int EventsMonth { get; set; }

    public string EventsMonthName { get; set; } = string.Empty;

    public List<PrincipalCalendarDayViewModel> CalendarDays { get; set; } = new List<PrincipalCalendarDayViewModel>();

    public List<PrincipalClassEventViewModel> UpcomingEvents { get; set; } = new List<PrincipalClassEventViewModel>();

    public List<PrincipalEventTypeOptionViewModel> EventTypeOptions { get; set; } = new List<PrincipalEventTypeOptionViewModel>();

    public List<PrincipalSubjectOptionViewModel> SubjectOptions { get; set; } = new List<PrincipalSubjectOptionViewModel>();

    public List<PrincipalClassStudentViewModel> Students { get; set; } = new List<PrincipalClassStudentViewModel>();
}

public class PrincipalTeacherOptionViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}

public class PrincipalClassSubjectViewModel
{
    public Guid ClassSubjectId { get; set; }

    public Guid SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public Guid? TeacherId { get; set; }

    public string? TeacherName { get; set; }
}

public class PrincipalSubjectOptionViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PrincipalClassStudentViewModel
{
    public Guid StudentId { get; set; }

    public Guid UserId { get; set; }

    public int ClassNumber { get; set; }

    public string FullName { get; set; } = string.Empty;

    public decimal? AverageGrade { get; set; }

    public int PraiseCount { get; set; }

    public int RemarkCount { get; set; }

    public int AbsenceAndLatenessCount { get; set; }
}

public class PrincipalScheduleDayViewModel
{
    public DayOfWeek DayOfWeek { get; set; }

    public string DayName { get; set; } = string.Empty;

    public int NextPeriodNumber { get; set; }

    public string NextPeriodTimeRange { get; set; } = string.Empty;

    public List<PrincipalScheduleEntryViewModel> Entries { get; set; } = new List<PrincipalScheduleEntryViewModel>();
}

public class PrincipalScheduleEntryViewModel
{
    public Guid Id { get; set; }

    public Guid ClassSubjectId { get; set; }

    public Guid? SubstituteTeacherId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int PeriodNumber { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string? TeacherName { get; set; }

    public string? SubstituteTeacherName { get; set; }

    public string TimeRange { get; set; } = string.Empty;
}

public class PrincipalScheduleConflictViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public Guid ClassId { get; set; }

    public string DayName { get; set; } = string.Empty;

    public int PeriodNumber { get; set; }

    public string TimeRange { get; set; } = string.Empty;
}

public class PrincipalScheduleTeacherOptionViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}

public class PrincipalCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public int DayNumber { get; set; }

    public bool IsCurrentMonth { get; set; }

    public bool IsToday { get; set; }

    public List<PrincipalClassEventViewModel> Events { get; set; } = new List<PrincipalClassEventViewModel>();
}

public class PrincipalClassEventViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string EventTypeName { get; set; } = string.Empty;

    public string EventTypeValue { get; set; } = string.Empty;

    public string EventTypeCssClass { get; set; } = string.Empty;

    public Guid? ClassSubjectId { get; set; }

    public string? ClassSubjectName { get; set; }

    public DateTime Date { get; set; }

    public string EditDateValue { get; set; } = string.Empty;

    public string DayLabel { get; set; } = string.Empty;

    public string TimeLabel { get; set; } = string.Empty;
}

public class PrincipalEventTypeOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
