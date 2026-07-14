using Entities.Enums;

namespace Entities.Models;

public class Notification
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public NotificationType Type { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    
    public bool IsRead { get; set; } = false;

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public Guid? GradeId { get; set; }
    public Grade? Grade { get; set; }

    public Guid? AbsenceId { get; set; }
    public Absence? Absence { get; set; }

    public Guid? FeedbackId { get; set; }
    public Feedback? Feedback { get; set; }

    public Guid? ChatId { get; set; }
    public Chat? Chat { get; set; }

    public Guid? MessageId { get; set; }
    public Message? Message { get; set; }
}
