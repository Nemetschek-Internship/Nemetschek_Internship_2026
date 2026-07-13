using Entities.Enums;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Repositories;

namespace Services.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IEmailService _emailService;
    private readonly INotificationPushService? _notificationPushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IEmailService emailService,
        ILogger<NotificationService> logger,
        INotificationPushService? notificationPushService = null)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _emailService = emailService;
        _logger = logger;
        _notificationPushService = notificationPushService;
    }

    public async Task<Guid> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        string text,
        Guid? eventId = null,
        Guid? gradeId = null,
        Guid? absenceId = null,
        Guid? feedbackId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                EventId = eventId,
                GradeId = gradeId,
                AbsenceId = absenceId,
                FeedbackId = feedbackId
            };

            await _notificationRepository.CreateAsync(notification, cancellationToken);
            _logger.LogInformation("Notification created for user {UserId} with type {NotificationType}", userId, type);

            if (_notificationPushService is not null)
            {
                await _notificationPushService.PushAsync(userId, MapToDto(notification), cancellationToken);
            }

            return notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _notificationRepository.GetAllAsync(cancellationToken);
            var userNotifications = notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(MapToDto)
                .ToList();

            return userNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _notificationRepository.GetAllAsync(cancellationToken);
            var unreadNotifications = notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(MapToDto)
                .ToList();

            return unreadNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", notificationId);
                return;
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _notificationRepository.GetAllAsync(cancellationToken);
            var userNotifications = notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();

            foreach (var notification in userNotifications)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
            }

            _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationRepository.DeleteAsync(notificationId, cancellationToken);
            _logger.LogInformation("Notification {NotificationId} deleted", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task BroadcastNotificationAsync(List<Guid> userIds, NotificationType type, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Broadcasting notification to {UserCount} users with type {NotificationType}", userIds.Count, type);

            var tasks = userIds.Select(userId =>
                CreateNotificationAsync(userId, type, text, cancellationToken: cancellationToken));

            await Task.WhenAll(tasks);

            _logger.LogInformation("Notification broadcast completed to {UserCount} users", userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification to users");
            throw;
        }
    }

    public async Task SendEmailNotificationAsync(Guid userId, string notificationTitle, string notificationMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for email notification", userId);
                return;
            }

            var recipientName = $"{user.FirstName} {user.LastName}";
            await _emailService.SendNotificationEmailAsync(user.Email, recipientName, notificationTitle, notificationMessage, cancellationToken);

            _logger.LogInformation("Email notification sent to user {UserId} at {Email}", userId, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task NotifyGradeAsync(Guid studentId, string subjectName, string gradeValue, List<string>? parentEmails = null, Guid? gradeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
            {
                _logger.LogWarning("Student {StudentId} not found for grade notification", studentId);
                return;
            }

            var studentUser = await _userRepository.GetByIdAsync(student.UserId, cancellationToken);
            if (studentUser == null)
            {
                _logger.LogWarning("Student user for student {StudentId} not found", studentId);
                return;
            }

            var notificationText = $"You received a new grade of {gradeValue} in {subjectName}.";
            await CreateNotificationAsync(studentUser.Id, NotificationType.Grade, notificationText, gradeId: gradeId, cancellationToken: cancellationToken);

            var studentName = $"{studentUser.FirstName} {studentUser.LastName}";
            await _emailService.SendNotificationEmailAsync(studentUser.Email, studentName, "New grade received", notificationText, cancellationToken);

            var parentIds = student.Parents.Where(parent => parent.User != null).Select(parent => parent.UserId).ToList();
            var parentEmailsFromStudent = student.Parents
                .Where(parent => parent.User != null && !string.IsNullOrWhiteSpace(parent.User.Email))
                .Select(parent => parent.User.Email!)
                .ToList();

            parentEmails ??= parentEmailsFromStudent;

            if (parentIds.Any())
            {
                await BroadcastNotificationAsync(parentIds, NotificationType.Grade, $"Your child received a new grade of {gradeValue} in {subjectName}.", cancellationToken);
            }

            if (parentEmails.Any())
            {
                var parentSubject = $"Your child received a new grade in {subjectName}";
                var parentMessage = $"Your child received a new grade of {gradeValue} in {subjectName}. Please sign in to NemeBook for details.";

                var emailTasks = parentEmails.Select(parentEmail =>
                    _emailService.SendNotificationEmailAsync(parentEmail, "Parent", parentSubject, parentMessage, cancellationToken));

                await Task.WhenAll(emailTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending grade notification emails for student {StudentId}", studentId);
            throw;
        }
    }

    private NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Text = notification.Text,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.IsRead,
            EventId = notification.EventId,
            GradeId = notification.GradeId,
            AbsenceId = notification.AbsenceId,
            FeedbackId = notification.FeedbackId
        };
    }
}
