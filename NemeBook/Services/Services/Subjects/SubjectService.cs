using Entities.Models;
using Services.Interfaces.Subjects;
using Services.Repositories;

namespace Services.Services.Subjects;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject id cannot be empty.", nameof(id));

        return _subjectRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _subjectRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        if (string.IsNullOrWhiteSpace(subject.Name))
            throw new ArgumentException("Subject name cannot be empty.", nameof(subject));

        if (subject.Name.Length > 100)
            throw new ArgumentException("Subject name cannot exceed 100 characters.", nameof(subject));

        await _subjectRepository.CreateAsync(subject, cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var existing = await _subjectRepository.GetByIdAsync(subject.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Subject not found.");

        await _subjectRepository.UpdateAsync(subject, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject id cannot be empty.", nameof(id));

        await _subjectRepository.DeleteAsync(id, cancellationToken);
    }
}