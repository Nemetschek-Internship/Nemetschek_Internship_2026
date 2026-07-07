using Entities.Models;

namespace Services.Repositories;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chat>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Chat chat, CancellationToken cancellationToken = default);

    Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
