using Entities.Models;
using Entities.ViewModels.Auth;

namespace Services.Interfaces;

public interface IAuthService
{
    Task<User?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
}
