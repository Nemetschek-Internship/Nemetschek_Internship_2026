using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly NemeBookDbContext dbContext;

    public FeedbackRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Feedbacks
            .FirstOrDefaultAsync(feedback => feedback.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Feedback>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Feedbacks
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Feedback feedback, CancellationToken cancellationToken = default)
    {
        await dbContext.Feedbacks.AddAsync(feedback, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Feedback feedback, CancellationToken cancellationToken = default)
    {
        dbContext.Feedbacks.Update(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var feedback = await GetByIdAsync(id, cancellationToken);
        if (feedback is null)
        {
            return;
        }

        dbContext.Feedbacks.Remove(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
