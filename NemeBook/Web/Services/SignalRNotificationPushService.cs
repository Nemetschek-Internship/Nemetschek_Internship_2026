using Microsoft.AspNetCore.SignalR;
using Services.Interfaces;
using Web.Hubs;

namespace Web.Services;

public class SignalRNotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationPushService> _logger;

    public SignalRNotificationPushService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationPushService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(userId.ToString()).SendAsync("notificationReceived", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push notification {NotificationId} to user {UserId}", notification.Id, userId);
        }
    }
}
