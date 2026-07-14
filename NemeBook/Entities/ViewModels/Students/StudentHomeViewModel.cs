namespace Entities.ViewModels.Students;

public class StudentHomeViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public string StudentInitials { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public decimal OverallAverage { get; set; }

    public int GradeCount { get; set; }

    public IReadOnlyList<StudentSubjectProgressItem> SubjectProgress { get; set; } = Array.Empty<StudentSubjectProgressItem>();

    public IReadOnlyList<StudentAcademicSubjectItem> AcademicSubjects { get; set; } = Array.Empty<StudentAcademicSubjectItem>();

    public IReadOnlyList<StudentTimelineItem> RecentGrades { get; set; } = Array.Empty<StudentTimelineItem>();

    public IReadOnlyList<StudentFeedbackDetailItem> Feedbacks { get; set; } = Array.Empty<StudentFeedbackDetailItem>();

    public StudentSummaryCard FeedbackSummary { get; set; } = new();

    public IReadOnlyList<StudentAbsenceDetailItem> Absences { get; set; } = Array.Empty<StudentAbsenceDetailItem>();

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

public class StudentAcademicSubjectItem
{
    public string SubjectName { get; set; } = string.Empty;

    public decimal Average { get; set; }

    public int GradeCount { get; set; }

    public IReadOnlyList<StudentGradeDetailItem> Grades { get; set; } = Array.Empty<StudentGradeDetailItem>();
}

public class StudentGradeDetailItem
{
    public string SubjectName { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public string Type { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Comment { get; set; } = string.Empty;
}

public class StudentFeedbackDetailItem
{
    public string SubjectName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Comment { get; set; } = string.Empty;
}

public class StudentAbsenceDetailItem
{
    public string SubjectName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public int LessonNumber { get; set; }

    public string ExcuseReason { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
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
