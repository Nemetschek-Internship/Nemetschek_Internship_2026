using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class ClassScheduleEntryRepository : IClassScheduleEntryRepository
{
    private readonly NemeBookDbContext dbContext;

    public ClassScheduleEntryRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<ClassScheduleEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ClassScheduleEntries
            .Include(scheduleEntry => scheduleEntry.Class)
            .Include(scheduleEntry => scheduleEntry.ClassSubject)
            .ThenInclude(classSubject => classSubject.Subject)
            .Include(scheduleEntry => scheduleEntry.ClassSubject)
            .ThenInclude(classSubject => classSubject.Teacher)
            .ThenInclude(teacher => teacher!.User)
            .Include(scheduleEntry => scheduleEntry.SubstituteTeacher)
            .ThenInclude(teacher => teacher!.User)
            .FirstOrDefaultAsync(scheduleEntry => scheduleEntry.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassScheduleEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ClassScheduleEntries
            .Include(scheduleEntry => scheduleEntry.Class)
            .Include(scheduleEntry => scheduleEntry.ClassSubject)
            .ThenInclude(classSubject => classSubject.Subject)
            .Include(scheduleEntry => scheduleEntry.ClassSubject)
            .ThenInclude(classSubject => classSubject.Teacher)
            .ThenInclude(teacher => teacher!.User)
            .Include(scheduleEntry => scheduleEntry.SubstituteTeacher)
            .ThenInclude(teacher => teacher!.User)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ClassScheduleEntry scheduleEntry, CancellationToken cancellationToken = default)
    {
        await dbContext.ClassScheduleEntries.AddAsync(scheduleEntry, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ClassScheduleEntry scheduleEntry, CancellationToken cancellationToken = default)
    {
        dbContext.ClassScheduleEntries.Update(scheduleEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scheduleEntry = await GetByIdAsync(id, cancellationToken);
        if (scheduleEntry is null)
        {
            return;
        }

        dbContext.ClassScheduleEntries.Remove(scheduleEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
