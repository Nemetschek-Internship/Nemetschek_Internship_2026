using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class TeacherRepository : ITeacherRepository
{
    private readonly NemeBookDbContext dbContext;

    public TeacherRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Teachers
            .Include(teacher => teacher.User)
            .Include(teacher => teacher.ClassSubjects)
            .FirstOrDefaultAsync(teacher => teacher.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Teachers
            .Include(teacher => teacher.User)
            .Include(teacher => teacher.ClassSubjects)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        await dbContext.Teachers.AddAsync(teacher, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        dbContext.Teachers.Update(teacher);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = await dbContext.Teachers
            .Include(existingTeacher => existingTeacher.User)
            .FirstOrDefaultAsync(existingTeacher => existingTeacher.Id == id, cancellationToken);

        if (teacher is null)
        {
            return;
        }

        teacher.User.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
