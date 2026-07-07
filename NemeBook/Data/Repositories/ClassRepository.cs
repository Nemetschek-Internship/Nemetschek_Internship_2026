using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly NemeBookDbContext dbContext;

    public ClassRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Classes
            .Include(schoolClass => schoolClass.Students)
            .ThenInclude(student => student.User)
            .FirstOrDefaultAsync(schoolClass => schoolClass.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Classes
            .Include(schoolClass => schoolClass.Students)
            .ThenInclude(student => student.User)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Class schoolClass, CancellationToken cancellationToken = default)
    {
        await dbContext.Classes.AddAsync(schoolClass, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Class schoolClass, CancellationToken cancellationToken = default)
    {
        dbContext.Classes.Update(schoolClass);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schoolClass = await GetByIdAsync(id, cancellationToken);
        if (schoolClass is null)
        {
            return;
        }

        dbContext.Classes.Remove(schoolClass);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
