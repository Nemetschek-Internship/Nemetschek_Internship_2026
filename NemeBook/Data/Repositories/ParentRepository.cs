using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class ParentRepository : IParentRepository
{
    private readonly NemeBookDbContext dbContext;

    public ParentRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Parents
            .FirstOrDefaultAsync(parent => parent.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Parents
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        await dbContext.Parents.AddAsync(parent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        dbContext.Parents.Update(parent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var parent = await GetByIdAsync(id, cancellationToken);
        if (parent is null)
        {
            return;
        }

        dbContext.Parents.Remove(parent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
