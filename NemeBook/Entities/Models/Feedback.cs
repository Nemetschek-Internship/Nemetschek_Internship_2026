using Entities.Enums;

namespace Entities.Models;

public class Feedback
{
    public Guid Id { get; set; }
    
    public Guid ClassSubjectId { get; set; }
    public ClassSubject ClassSubject { get; set; } = null!;
    
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime Date { get; set; }
    
    public FeedbackType Type { get; set; }

    public string Description { get; set; } = string.Empty;
}