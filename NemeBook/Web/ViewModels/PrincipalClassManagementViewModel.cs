namespace Web.ViewModels;

public class PrincipalClassManagementViewModel
{
    public Guid ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string ActiveTab { get; set; } = "Students";

    public string SectionTitle { get; set; } = "Ученици";

    public string? EmptyMessage { get; set; }

    public List<PrincipalClassStudentViewModel> Students { get; set; } = new List<PrincipalClassStudentViewModel>();
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
