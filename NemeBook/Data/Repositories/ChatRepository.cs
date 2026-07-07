using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly NemeBookDbContext dbContext;

    public ChatRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Chat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Chats
            .Include(chat => chat.Users)
            .FirstOrDefaultAsync(chat => chat.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Chat>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Chats
            .Include(chat => chat.Users)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        await dbContext.Chats.AddAsync(chat, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        dbContext.Chats.Update(chat);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var chat = await GetByIdAsync(id, cancellationToken);
        if (chat is null)
        {
            return;
        }

        dbContext.Chats.Remove(chat);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
