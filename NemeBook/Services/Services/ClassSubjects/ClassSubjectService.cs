using Entities.Models;
using Services.Interfaces.ClassSubjects;
using Services.Repositories;

namespace Services.Services.ClassSubjects;

public class ClassSubjectService : IClassSubjectService
{
    private readonly IClassSubjectRepository _classSubjectRepository;

    public ClassSubjectService(IClassSubjectRepository classSubjectRepository)
    {
        _classSubjectRepository = classSubjectRepository;
    }

    public Task<ClassSubject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ClassSubject id cannot be empty.", nameof(id));

        return _classSubjectRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<ClassSubject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _classSubjectRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(classSubject);

        if (classSubject.ClassId == Guid.Empty)
            throw new ArgumentException("ClassSubject ClassId cannot be empty.", nameof(classSubject));

        if (classSubject.SubjectId == Guid.Empty)
            throw new ArgumentException("ClassSubject SubjectId cannot be empty.", nameof(classSubject));

        if (classSubject.TeacherId == Guid.Empty)
            throw new ArgumentException("ClassSubject TeacherId cannot be empty.", nameof(classSubject));

        await _classSubjectRepository.CreateAsync(classSubject, cancellationToken);
    }

    public async Task UpdateAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(classSubject);

        var existing = await _classSubjectRepository.GetByIdAsync(classSubject.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("ClassSubject not found.");

        await _classSubjectRepository.UpdateAsync(classSubject, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ClassSubject id cannot be empty.", nameof(id));

        await _classSubjectRepository.DeleteAsync(id, cancellationToken);
    }
}