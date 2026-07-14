using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly NemeBookDbContext dbContext;

    public StudentRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Students
            .Include(student => student.User)
            .Include(student => student.Class)
            .Include(student => student.Grades)
            .Include(student => student.Feedbacks)
            .Include(student => student.Absences)
            .Include(student => student.Parents)
            .FirstOrDefaultAsync(student => student.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Students
            .Include(student => student.User)
            .Include(student => student.Class)
            .Include(student => student.Grades)
            .Include(student => student.Feedbacks)
            .Include(student => student.Absences)
            .Include(student => student.Parents)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        await dbContext.Students.AddAsync(student, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        dbContext.Students.Update(student);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await dbContext.Students
            .Include(existingStudent => existingStudent.User)
            .FirstOrDefaultAsync(existingStudent => existingStudent.Id == id, cancellationToken);

        if (student is null)
        {
            return;
        }

        student.User.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
