namespace Entities.ViewModels.Grades;

public class BulkCreateGradeResult
{
    public List<GradeDto> CreatedGrades { get; set; } = new();
    public List<BulkGradeError> Errors { get; set; } = new();
}

public class BulkGradeError
{
    public Guid StudentId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
