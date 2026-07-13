using Entities.Enums;

namespace Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Creates and sends a notification to a user
    /// </summary>
    Task<Guid> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        string text,
        Guid? eventId = null,
        Guid? gradeId = null,
        Guid? absenceId = null,
        Guid? feedbackId = null,
        Guid? chatId = null,
        Guid? messageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications for a user
    /// </summary>
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications for a user as read
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <summary>
    /// Broadcasts a notification to multiple users
    /// </summary>
    Task BroadcastNotificationAsync(List<Guid> userIds, NotificationType type, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a grade notification for a student, then sends instant email to the student and parents.
    /// </summary>
    Task NotifyGradeAsync(Guid studentId, string subjectName, string gradeValue, List<string>? parentEmails = null, Guid? gradeId = null, CancellationToken cancellationToken = default);
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public Guid? EventId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? AbsenceId { get; set; }
    public Guid? FeedbackId { get; set; }
    public Guid? ChatId { get; set; }
    public Guid? MessageId { get; set; }
}
