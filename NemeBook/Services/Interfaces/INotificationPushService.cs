namespace Services.Interfaces;

public interface INotificationPushService
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
