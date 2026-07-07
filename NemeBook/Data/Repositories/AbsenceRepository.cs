using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class AbsenceRepository : IAbsenceRepository
{
    private readonly NemeBookDbContext dbContext;

    public AbsenceRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Absence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Absences
            .FirstOrDefaultAsync(absence => absence.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Absence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Absences
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Absence absence, CancellationToken cancellationToken = default)
    {
        await dbContext.Absences.AddAsync(absence, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Absence absence, CancellationToken cancellationToken = default)
    {
        dbContext.Absences.Update(absence);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var absence = await GetByIdAsync(id, cancellationToken);
        if (absence is null)
        {
            return;
        }

        dbContext.Absences.Remove(absence);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
