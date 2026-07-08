using Entities.Enums;

namespace Entities.ViewModels.Grades;

public class StudentGradesViewModel
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;

    public Dictionary<string, List<GradeDto>> GradesBySubject { get; set; } = new();
    public Dictionary<string, decimal> AverageBySubject { get; set; } = new();
    public decimal OverallAverage { get; set; }
    public List<GradeTypeSummaryDto> TypeSummary { get; set; } = new();
}

public class GradeTypeSummaryDto
{
    public GradeType Type { get; set; }
    public int Count { get; set; }
    public decimal Average { get; set; }
}