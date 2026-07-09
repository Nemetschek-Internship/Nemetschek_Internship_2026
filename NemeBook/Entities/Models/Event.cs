using Entities.Enums;

namespace Entities.Models;

public class Event
{
    public Guid Id { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid? ClassSubjectId { get; set; }
    public ClassSubject? ClassSubject { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
        
    public EventType EventType { get; set; }
    
    public DateTime Date { get; set; }

    public List<Class> Classes { get; set; } = new List<Class>();
}
