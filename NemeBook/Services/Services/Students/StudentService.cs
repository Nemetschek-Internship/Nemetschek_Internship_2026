using Entities.Models;
using Services.Interfaces.Students;
using Services.Repositories;

namespace Services.Services.Students;

public class StudentService : IStudentService
{
    private readonly IStudentRepository studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        this.studentRepository = studentRepository;
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на ученика не може да бъде празен.", nameof(id));
        }

        return studentRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return studentRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.UserId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на потребителя за този ученик не може да бъде празен.", nameof(student));
        }

        if (student.ClassId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на класа за този ученик не може да бъде празен.", nameof(student));
        }

        await studentRepository.CreateAsync(student, cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.Id == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на ученика не може да бъде празен.", nameof(student));
        }

        var existingStudent = await studentRepository.GetByIdAsync(student.Id, cancellationToken);
        if (existingStudent is null)
        {
            throw new InvalidOperationException("Ученикът не беше намерен.");
        }

        await studentRepository.UpdateAsync(student, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на ученика не може да бъде празен.", nameof(id));
        }

        var existingStudent = await studentRepository.GetByIdAsync(id, cancellationToken);
        if (existingStudent is null)
        {
            return;
        }

        await studentRepository.DeleteAsync(id, cancellationToken);
    }
}
