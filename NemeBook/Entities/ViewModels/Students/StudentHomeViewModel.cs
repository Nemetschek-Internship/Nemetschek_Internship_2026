namespace Entities.ViewModels.Students;

public class StudentHomeViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentInitials { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public decimal OverallAverage { get; set; }

    public int GradeCount { get; set; }

    public IReadOnlyList<StudentSubjectProgressItem> SubjectProgress { get; set; } = Array.Empty<StudentSubjectProgressItem>();

    public IReadOnlyList<StudentTimelineItem> RecentGrades { get; set; } = Array.Empty<StudentTimelineItem>();

    public StudentSummaryCard FeedbackSummary { get; set; } = new();

    public StudentSummaryCard AbsenceSummary { get; set; } = new();

    public IReadOnlyList<StudentScheduleItem> TodaysSchedule { get; set; } = Array.Empty<StudentScheduleItem>();
}

public class StudentSubjectProgressItem
{
    public string SubjectName { get; set; } = string.Empty;

    public decimal Average { get; set; }

    public int GradeCount { get; set; }

    public int ExcellentGradeCount { get; set; }

    public int ProgressPercent { get; set; }

    public string AccentClass { get; set; } = string.Empty;
}

public class StudentTimelineItem
{
    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class StudentSummaryCard
{
    public int Count { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}

public class StudentScheduleItem
{
    public int PeriodNumber { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string TimeRange { get; set; } = string.Empty;
}
