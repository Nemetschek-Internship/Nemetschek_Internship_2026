using Entities.Enums;

namespace Entities.Models;

public class Absence
{
    public Guid Id { get; set; }
    
    public Guid ClassSubjectId { get; set; }
    public ClassSubject ClassSubject { get; set; } = null!;
    
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime Date { get; set; }
    
    public int LessonNumber { get; set; } // Hour Number

    public AbsenceType Type { get; set; }

    public AbsenceStatus Status { get; set; }

    public AbsenceExcuseReason? ExcuseReason { get; set; }

    public string Description { get; set; } = string.Empty;
}
