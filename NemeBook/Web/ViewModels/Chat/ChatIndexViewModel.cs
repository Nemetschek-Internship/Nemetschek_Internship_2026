using Entities.Enums;

namespace Web.ViewModels.Chat;

public class ChatIndexViewModel
{
    public Guid CurrentUserId { get; set; }

    public string CurrentUserName { get; set; } = string.Empty;

    public string CurrentUserInitials { get; set; } = string.Empty;

    public string CurrentUserRole { get; set; } = string.Empty;

    public Guid? PendingTargetUserId { get; set; }

    public IReadOnlyList<ChatListItemViewModel> Chats { get; set; } = Array.Empty<ChatListItemViewModel>();

    public IReadOnlyList<ChatContactViewModel> Contacts { get; set; } = Array.Empty<ChatContactViewModel>();
}

public class ChatListItemViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string? LastMessage { get; set; }

    public string? LastMessageTime { get; set; }

    public DateTime? LastActivityAt { get; set; }
}

public class ChatContactViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class StartDirectChatRequest
{
    public Guid TargetUserId { get; set; }
}

public class ChatMessageViewModel
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public Guid SenderId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
}
