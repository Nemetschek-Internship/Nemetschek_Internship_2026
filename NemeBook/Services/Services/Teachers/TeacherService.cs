using Entities.Models;
using Services.Interfaces.Teachers;
using Services.Repositories;

namespace Services.Services.Teachers;

public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _teacherRepository;

    public TeacherService(ITeacherRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    public Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификаторът на учителя не може да бъде празен.", nameof(id));

        return _teacherRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Teacher>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _teacherRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        if (teacher.UserId == Guid.Empty)
            throw new ArgumentException("Идентификаторът на потребителя за този учител не може да бъде празен.", nameof(teacher));

        await _teacherRepository.CreateAsync(teacher, cancellationToken);
    }

    public async Task UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        var existing = await _teacherRepository.GetByIdAsync(teacher.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Учителят не беше намерен.");

        await _teacherRepository.UpdateAsync(teacher, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификаторът на учителя не може да бъде празен.", nameof(id));

        await _teacherRepository.DeleteAsync(id, cancellationToken);
    }
}
