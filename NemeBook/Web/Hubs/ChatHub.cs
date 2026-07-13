using System.Security.Claims;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Services.Interfaces;
using Services.Interfaces.Chats;
using Web.ViewModels.Chat;

namespace Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IAuthService authService;
    private readonly IChatService chatService;

    public ChatHub(IAuthService authService, IChatService chatService)
    {
        this.authService = authService;
        this.chatService = chatService;
    }

    public async Task JoinChat(Guid chatId)
    {
        var userId = GetCurrentUserId();
        var messages = await chatService.GetMessagesAsync(userId, chatId);

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(chatId));
        await Clients.Caller.SendAsync("ChatReady", chatId, messages.Select(message => MapMessage(message)));
    }

    public async Task SendMessage(Guid chatId, string text)
    {
        var userId = GetCurrentUserId();
        var user = await authService.GetUserByIdAsync(userId)
            ?? throw new HubException("Потребителят не е намерен.");

        var message = await chatService.SendMessageAsync(userId, chatId, text);
        await Clients.Group(GetGroupName(chatId)).SendAsync("ReceiveMessage", MapMessage(message, user));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new HubException("Трябва да влезете в профила си.");
        }

        return userId;
    }

    private static string GetGroupName(Guid chatId)
    {
        return $"chat-{chatId:N}";
    }

    private static ChatMessageViewModel MapMessage(Message message, User? senderFallback = null)
    {
        var sender = message.Sender ?? senderFallback;

        return new ChatMessageViewModel
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = sender is null ? "Потребител" : FormatFullName(sender),
            Text = message.Text,
            SentAt = message.SentAt
        };
    }

    private static string FormatFullName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
