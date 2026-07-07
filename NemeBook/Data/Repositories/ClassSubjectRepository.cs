using Entities.Models;
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
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ClassSubject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
