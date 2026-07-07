using Entities.Models;

namespace Services.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<Chat>> GetChatsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid requesterUserId, Guid chatId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> SearchAvailableContactsAsync(Guid requesterUserId, string? searchTerm, CancellationToken cancellationToken = default);

    Task<Chat> GetOrCreateDirectChatAsync(Guid requesterUserId, Guid targetUserId, CancellationToken cancellationToken = default);

    Task<Chat> GetOrCreateClassChatAsync(Guid creatorUserId, Guid classId, CancellationToken cancellationToken = default);

    Task<Chat> GetOrCreateTeachersGroupChatAsync(Guid creatorUserId, CancellationToken cancellationToken = default);

    Task<Message> SendMessageAsync(Guid senderUserId, Guid chatId, string text, CancellationToken cancellationToken = default);
}
