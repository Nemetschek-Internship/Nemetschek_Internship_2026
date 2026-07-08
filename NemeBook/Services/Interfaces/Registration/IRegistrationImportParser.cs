using Services.Dtos.Registration;

namespace Services.Interfaces.Registration;

public interface IRegistrationImportParser
{
    Task<IReadOnlyList<StudentImportDto>> ParseStudentsAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherImportDto>> ParseTeachersAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParentImportDto>> ParseParentsAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
