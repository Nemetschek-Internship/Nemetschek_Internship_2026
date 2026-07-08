namespace Entities.ViewModels.Grades;

public class ClassGradesViewModel
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;

    public List<StudentGradeRowDto> StudentGrades { get; set; } = new();
    public ClassGradeSummaryDto Summary { get; set; } = new();
}

public class StudentGradeRowDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<GradeDto> Grades { get; set; } = new();
    public decimal Average { get; set; }
    public int TotalGrades { get; set; }
    public GradeDto? LatestGrade { get; set; }
}

public class ClassGradeSummaryDto
{
    public int TotalStudents { get; set; }
    public decimal ClassAverage { get; set; }
    public decimal? MinGrade { get; set; }
    public decimal? MaxGrade { get; set; }
    public Dictionary<int, int> GradeDistribution { get; set; } = new();
}