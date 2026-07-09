using Entities.Enums;
using Services.Dtos.Registration;

namespace Services.Interfaces.Registration;

public interface IRegistrationService
{
    Task<RegistrationImportResult> ImportStudentsAsync(
        IReadOnlyCollection<StudentImportDto> students,
        CancellationToken cancellationToken = default);

    Task<RegistrationImportResult> ImportTeachersAsync(
        IReadOnlyCollection<TeacherImportDto> teachers,
        CancellationToken cancellationToken = default);

    Task<RegistrationImportResult> ImportParentsAsync(
        IReadOnlyCollection<ParentImportDto> parents,
        CancellationToken cancellationToken = default);

    Task CompleteSetPasswordAsync(
        CompleteSetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task CompleteParentSignUpAsync(
        CompleteParentSignUpRequest request,
        CancellationToken cancellationToken = default);

    Task ValidateInvitationAsync(
        string token,
        RegistrationInvitationType type,
        CancellationToken cancellationToken = default);

    Task<PrincipalSeedResult> SeedPrincipalAsync(
        SeedPrincipalRequest request,
        CancellationToken cancellationToken = default);

    Task<PrincipalSeedResult> SeedUserAsync(
        SeedPrincipalRequest request,
        UserRole role,
        CancellationToken cancellationToken = default);
}
