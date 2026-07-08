using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly NemeBookDbContext dbContext;

    public SubjectRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Subjects
            .Include(subject => subject.TeacherSubjects)
            .FirstOrDefaultAsync(subject => subject.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Subjects
            .Include(subject => subject.TeacherSubjects)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        await dbContext.Subjects.AddAsync(subject, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        dbContext.Subjects.Update(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await GetByIdAsync(id, cancellationToken);
        if (subject is null)
        {
            return;
        }

        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
