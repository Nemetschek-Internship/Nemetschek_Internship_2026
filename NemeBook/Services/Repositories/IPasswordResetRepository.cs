using Entities.Models;

namespace Services.Repositories;

public interface IPasswordResetRepository
{
    Task CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    Task CreateOrReplaceAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
