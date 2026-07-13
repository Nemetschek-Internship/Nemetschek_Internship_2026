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
