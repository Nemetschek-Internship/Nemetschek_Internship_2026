using Entities.Models;
using Services.Interfaces.Classes;
using Services.Repositories;

namespace Services.Services.Classes;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;

    public ClassService(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public Task<Class?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Class id cannot be empty.", nameof(id));

        return _classRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Class>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _classRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(Class classEntity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(classEntity);

        if (classEntity.GradeNumber < 1 || classEntity.GradeNumber > 12)
            throw new ArgumentException("Grade number must be between 1 and 12.", nameof(classEntity));

        if (!char.IsLetter(classEntity.Letter))
            throw new ArgumentException("Letter must be a valid character.", nameof(classEntity));

        await _classRepository.CreateAsync(classEntity, cancellationToken);
    }

    public async Task UpdateAsync(Class classEntity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(classEntity);

        var existing = await _classRepository.GetByIdAsync(classEntity.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Class not found.");

        await _classRepository.UpdateAsync(classEntity, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Class id cannot be empty.", nameof(id));

        await _classRepository.DeleteAsync(id, cancellationToken);
    }
}