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
            throw new ArgumentException("Идентификаторът на предмета не може да бъде празен.", nameof(id));

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
            throw new ArgumentException("Името на предмета не може да бъде празно.", nameof(subject));

        if (subject.Name.Length > 100)
            throw new ArgumentException("Името на предмета не може да бъде по-дълго от 100 символа.", nameof(subject));

        await _subjectRepository.CreateAsync(subject, cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var existing = await _subjectRepository.GetByIdAsync(subject.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Предметът не беше намерен.");

        await _subjectRepository.UpdateAsync(subject, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификаторът на предмета не може да бъде празен.", nameof(id));

        await _subjectRepository.DeleteAsync(id, cancellationToken);
    }
}
