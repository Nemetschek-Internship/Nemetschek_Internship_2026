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
            .Include(parent => parent.User)
            .Include(parent => parent.Students)
            .ThenInclude(student => student.User)
            .FirstOrDefaultAsync(parent => parent.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Parents
            .Include(parent => parent.User)
            .Include(parent => parent.Students)
            .ThenInclude(student => student.User)
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
        var parent = await dbContext.Parents
            .Include(existingParent => existingParent.User)
            .FirstOrDefaultAsync(existingParent => existingParent.Id == id, cancellationToken);

        if (parent is null)
        {
            return;
        }

        parent.User.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
