using Entities.Enums;

namespace Entities.ViewModels.Grades;

public class GradeFilterRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public GradeType? Type { get; set; }
    public Guid? SubjectId { get; set; }
    public bool ShowOnlyLatest { get; set; }
}