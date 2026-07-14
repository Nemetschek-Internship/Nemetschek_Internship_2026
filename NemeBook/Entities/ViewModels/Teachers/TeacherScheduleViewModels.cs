using Entities.Enums;

namespace Entities.ViewModels.Teachers;

public class TeacherScheduleViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public IReadOnlyList<TeacherScheduleDayViewModel> Days { get; set; } = Array.Empty<TeacherScheduleDayViewModel>();
}

public class TeacherScheduleDayViewModel
{
    public DayOfWeek DayOfWeek { get; set; }

    public string DayName { get; set; } = string.Empty;

    public IReadOnlyList<TeacherScheduleEntryViewModel> Entries { get; set; } = Array.Empty<TeacherScheduleEntryViewModel>();
}

public class TeacherScheduleEntryViewModel
{
    public Guid Id { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int PeriodNumber { get; set; }

    public string TimeRange { get; set; } = string.Empty;

    public bool IsSubstitution { get; set; }
}

public class TeacherCalendarViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public int Year { get; set; }

    public int Month { get; set; }

    public string MonthName { get; set; } = string.Empty;

    public IReadOnlyList<TeacherCalendarDayViewModel> CalendarDays { get; set; } = Array.Empty<TeacherCalendarDayViewModel>();

    public IReadOnlyList<TeacherCalendarEventViewModel> UpcomingEvents { get; set; } = Array.Empty<TeacherCalendarEventViewModel>();
}

public class TeacherCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public int DayNumber { get; set; }

    public bool IsCurrentMonth { get; set; }

    public bool IsToday { get; set; }

    public IReadOnlyList<TeacherCalendarEventViewModel> Events { get; set; } = Array.Empty<TeacherCalendarEventViewModel>();
}

public class TeacherCalendarEventViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EventTypeName { get; set; } = string.Empty;

    public string EventTypeCssClass { get; set; } = string.Empty;

    public string? ClassSubjectName { get; set; }

    public string ClassNames { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string DayLabel { get; set; } = string.Empty;

    public string TimeLabel { get; set; } = string.Empty;
}
