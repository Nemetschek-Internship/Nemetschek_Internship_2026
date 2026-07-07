using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly NemeBookDbContext dbContext;

    public GradeRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Grades
            .FirstOrDefaultAsync(grade => grade.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Grade>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Grades
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        await dbContext.Grades.AddAsync(grade, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        dbContext.Grades.Update(grade);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var grade = await GetByIdAsync(id, cancellationToken);
        if (grade is null)
        {
            return;
        }

        dbContext.Grades.Remove(grade);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
