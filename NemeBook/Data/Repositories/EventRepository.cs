using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly NemeBookDbContext dbContext;

    public EventRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Events
            .Include(schoolEvent => schoolEvent.Classes)
            .Include(schoolEvent => schoolEvent.ClassSubject)
            .ThenInclude(classSubject => classSubject!.Subject)
            .FirstOrDefaultAsync(schoolEvent => schoolEvent.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .Include(schoolEvent => schoolEvent.Classes)
            .Include(schoolEvent => schoolEvent.ClassSubject)
            .ThenInclude(classSubject => classSubject!.Subject)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Event schoolEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.Events.AddAsync(schoolEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event schoolEvent, CancellationToken cancellationToken = default)
    {
        dbContext.Events.Update(schoolEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schoolEvent = await GetByIdAsync(id, cancellationToken);
        if (schoolEvent is null)
        {
            return;
        }

        dbContext.Events.Remove(schoolEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
