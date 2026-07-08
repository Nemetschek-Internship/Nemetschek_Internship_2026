using Entities.Enums;

namespace Entities.ViewModels.Grades;

public class GradeDto
{
    public Guid Id { get; set; }
    public decimal Value { get; set; }
    public GradeType Type { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    // Subject info
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;

    // Teacher info
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;

    // Student info
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
}