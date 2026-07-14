using System.Security.Claims;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Interfaces.Chats;
using Web.ViewModels.Chat;

namespace Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IAuthService authService;
    private readonly IChatService chatService;
    private readonly ILogger<ChatController> logger;

    public ChatController(
        IAuthService authService,
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        this.authService = authService;
        this.chatService = chatService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? targetUserId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var chats = await GetChatsForUserSafelyAsync(user.Id, cancellationToken);

        var model = new ChatIndexViewModel
        {
            CurrentUserId = user.Id,
            CurrentUserName = FormatFullName(user),
            CurrentUserInitials = FormatInitials(user.FirstName, user.LastName),
            CurrentUserRole = GetRoleDisplayName(user.Role),
            PendingTargetUserId = targetUserId,
            Chats = await MapChatsAsync(user.Id, chats, cancellationToken)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? term, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var contacts = await SearchContactsSafelyAsync(userId.Value, term, cancellationToken);
        return Json(contacts.Select(MapContact));
    }

    [HttpPost]
    public async Task<IActionResult> Direct([FromBody] StartDirectChatRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        if (request.TargetUserId == Guid.Empty)
        {
            return BadRequest("Изберете потребител.");
        }

        try
        {
            var chat = await chatService.GetOrCreateDirectChatAsync(userId.Value, request.TargetUserId, cancellationToken);
            return Json(await MapChatAsync(userId.Value, chat, cancellationToken));
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
            logger.LogWarning(ex, "Direct chat creation failed for user {UserId}", userId.Value);
            return BadRequest("Не можете да започнете чат с този потребител.");
        }
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        return userId.HasValue
            ? await authService.GetUserByIdAsync(userId.Value, cancellationToken)
            : null;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private async Task<IReadOnlyList<Chat>> GetChatsForUserSafelyAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await chatService.GetChatsForUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to load chats for user {UserId}", userId);
            return Array.Empty<Chat>();
        }
    }

    private async Task<IReadOnlyList<User>> SearchContactsSafelyAsync(
        Guid userId,
        string? term,
        CancellationToken cancellationToken)
    {
        try
        {
            return await chatService.SearchAvailableContactsAsync(userId, term, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to search chat contacts for user {UserId}", userId);
            return Array.Empty<User>();
        }
    }

    private async Task<IReadOnlyList<ChatListItemViewModel>> MapChatsAsync(
        Guid currentUserId,
        IReadOnlyList<Chat> chats,
        CancellationToken cancellationToken)
    {
        var items = new List<ChatListItemViewModel>();

        foreach (var chat in chats)
        {
            items.Add(await MapChatAsync(currentUserId, chat, cancellationToken));
        }

        return items
            .OrderByDescending(item => item.LastActivityAt ?? DateTime.MinValue)
            .ThenBy(item => item.Title)
            .ToList();
    }

    private async Task<ChatListItemViewModel> MapChatAsync(
        Guid currentUserId,
        Chat chat,
        CancellationToken cancellationToken)
    {
        var messages = await chatService.GetMessagesAsync(currentUserId, chat.Id, cancellationToken);
        var lastMessage = messages.LastOrDefault();
        var title = GetChatTitle(currentUserId, chat);

        return new ChatListItemViewModel
        {
            Id = chat.Id,
            Title = title,
            Subtitle = GetChatSubtitle(chat),
            Initials = GetChatInitials(currentUserId, chat, title),
            LastMessage = lastMessage?.Text,
            LastMessageTime = lastMessage?.SentAt.ToLocalTime().ToString("HH:mm"),
            LastActivityAt = lastMessage?.SentAt
        };
    }

    private static ChatContactViewModel MapContact(User user)
    {
        return new ChatContactViewModel
        {
            Id = user.Id,
            FullName = FormatFullName(user),
            Initials = FormatInitials(user.FirstName, user.LastName),
            Role = GetRoleDisplayName(user.Role)
        };
    }

    private static string GetChatTitle(Guid currentUserId, Chat chat)
    {
        if (chat.Name == "TEACHERS:GROUP")
        {
            return "Учителска група";
        }

        if (chat.Name?.StartsWith("CLASS:", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Класен чат";
        }

        if (!string.IsNullOrWhiteSpace(chat.Name))
        {
            return chat.Name;
        }

        var otherUser = chat.Users.FirstOrDefault(user => user.Id != currentUserId);
        return otherUser is null ? "Чат" : FormatFullName(otherUser);
    }

    private static string GetChatSubtitle(Chat chat)
    {
        if (chat.Name == "TEACHERS:GROUP")
        {
            return "Общ чат за учители";
        }

        if (chat.Name?.StartsWith("CLASS:", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Групов чат";
        }

        return chat.Users.Count <= 2 ? "Директен чат" : $"{chat.Users.Count} участници";
    }

    private static string GetChatInitials(Guid currentUserId, Chat chat, string title)
    {
        if (!string.IsNullOrWhiteSpace(chat.Name))
        {
            return FormatTitleInitials(title);
        }

        var otherUser = chat.Users.FirstOrDefault(user => user.Id != currentUserId);
        return otherUser is not null
            ? FormatInitials(otherUser.FirstName, otherUser.LastName)
            : FormatTitleInitials(title);
    }

    private static string FormatFullName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatInitials(string firstName, string lastName)
    {
        var firstInitial = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName[0].ToString();
        var lastInitial = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName[0].ToString();
        return $"{firstInitial}{lastInitial}".ToUpperInvariant();
    }

    private static string FormatTitleInitials(string title)
    {
        var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(string.Empty, parts.Take(2).Select(part => part[0])).ToUpperInvariant();
    }

    private static string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Student => "Ученик",
            UserRole.Parent => "Родител",
            UserRole.Teacher => "Учител",
            UserRole.Principal => "Директор",
            _ => role.ToString()
        };
    }
}
