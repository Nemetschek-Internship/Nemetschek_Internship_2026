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

    public List<PrincipalClassSubjectViewModel> ClassSubjects { get; set; } = new List<PrincipalClassSubjectViewModel>();

    public List<PrincipalScheduleDayViewModel> ScheduleDays { get; set; } = new List<PrincipalScheduleDayViewModel>();

    public PrincipalScheduleConflictViewModel? ScheduleConflict { get; set; }

    public List<PrincipalSubjectOptionViewModel> SubjectOptions { get; set; } = new List<PrincipalSubjectOptionViewModel>();

    public List<PrincipalClassStudentViewModel> Students { get; set; } = new List<PrincipalClassStudentViewModel>();
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
