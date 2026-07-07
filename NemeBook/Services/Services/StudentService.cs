using Entities.Models;
using Services.Interfaces;
using Services.Repositories;

namespace Services.Services;

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
            throw new ArgumentException("Student id cannot be empty.", nameof(id));
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
            throw new ArgumentException("Student UserId cannot be empty.", nameof(student));
        }

        if (student.ClassId == Guid.Empty)
        {
            throw new ArgumentException("Student ClassId cannot be empty.", nameof(student));
        }

        await studentRepository.CreateAsync(student, cancellationToken);
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.Id == Guid.Empty)
        {
            throw new ArgumentException("Student id cannot be empty.", nameof(student));
        }

        var existingStudent = await studentRepository.GetByIdAsync(student.Id, cancellationToken);
        if (existingStudent is null)
        {
            throw new InvalidOperationException("Student was not found.");
        }

        await studentRepository.UpdateAsync(student, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Student id cannot be empty.", nameof(id));
        }

        var existingStudent = await studentRepository.GetByIdAsync(id, cancellationToken);
        if (existingStudent is null)
        {
            return;
        }

        await studentRepository.DeleteAsync(id, cancellationToken);
    }
}
