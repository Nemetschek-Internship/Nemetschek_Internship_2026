using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly NemeBookDbContext dbContext;

    public MessageRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Messages
            .Include(message => message.Sender)
            .FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Messages
            .Include(message => message.Sender)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Message message, CancellationToken cancellationToken = default)
    {
        await dbContext.Messages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        dbContext.Messages.Update(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return;
        }

        dbContext.Messages.Remove(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
