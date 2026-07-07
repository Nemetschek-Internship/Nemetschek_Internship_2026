using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class ClassSubjectRepository : IClassSubjectRepository
{
    private readonly NemeBookDbContext dbContext;

    public ClassSubjectRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<ClassSubject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ClassSubjects
            .Include(classSubject => classSubject.Class)
            .Include(classSubject => classSubject.Teacher)
            .FirstOrDefaultAsync(classSubject => classSubject.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSubject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ClassSubjects
            .Include(classSubject => classSubject.Class)
            .Include(classSubject => classSubject.Teacher)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        await dbContext.ClassSubjects.AddAsync(classSubject, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        dbContext.ClassSubjects.Update(classSubject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var classSubject = await GetByIdAsync(id, cancellationToken);
        if (classSubject is null)
        {
            return;
        }

        dbContext.ClassSubjects.Remove(classSubject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
