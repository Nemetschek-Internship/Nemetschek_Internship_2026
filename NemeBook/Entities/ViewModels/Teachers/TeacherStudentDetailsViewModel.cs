namespace Entities.ViewModels.Teachers;

public class TeacherStudentDetailsViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string StudentInitials { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public bool CanManageRecords { get; set; }

    public IReadOnlyList<TeacherStudentGradeDetailsItem> Grades { get; set; } = Array.Empty<TeacherStudentGradeDetailsItem>();

    public IReadOnlyList<TeacherStudentFeedbackDetailsItem> Feedbacks { get; set; } = Array.Empty<TeacherStudentFeedbackDetailsItem>();

    public IReadOnlyList<TeacherStudentAbsenceDetailsItem> Absences { get; set; } = Array.Empty<TeacherStudentAbsenceDetailsItem>();
}

public class TeacherStudentGradeDetailsItem
{
    public Guid Id { get; set; }

    public Guid ClassSubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public int DisplayValue { get; set; }

    public int TypeValue { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class TeacherStudentAbsenceDetailsItem
{
    public Guid Id { get; set; }

    public Guid ClassSubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public int LessonNumber { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public int TypeValue { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public int StatusValue { get; set; }

    public bool IsExcused { get; set; }

    public int? ExcuseReasonValue { get; set; }

    public string ExcuseReasonName { get; set; } = string.Empty;

    public string ExcuseNote { get; set; } = string.Empty;

    public bool CanExcuse { get; set; }
}

public class TeacherStudentFeedbackDetailsItem
{
    public Guid Id { get; set; }

    public Guid ClassSubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public int TypeValue { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool CanManage { get; set; }
}

public class TeacherStudentFeedbacksViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string StudentInitials { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public IReadOnlyList<TeacherStudentFeedbackDetailsItem> Feedbacks { get; set; } = Array.Empty<TeacherStudentFeedbackDetailsItem>();
}

public class TeacherStudentAbsencesViewModel
{
    public string TeacherName { get; set; } = string.Empty;

    public string TeacherInitials { get; set; } = string.Empty;

    public string MainMeta { get; set; } = "Учител";

    public Guid StudentId { get; set; }

    public Guid ClassId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string StudentInitials { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public IReadOnlyList<TeacherStudentAbsenceDetailsItem> Absences { get; set; } = Array.Empty<TeacherStudentAbsenceDetailsItem>();
}
