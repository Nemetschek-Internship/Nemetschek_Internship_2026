using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Auth;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Repositories;
using Services.Interfaces.Security;

namespace Services.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        ILogger<AuthService> logger,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return null;
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("Login failed - user is deleted: {Email}", request.Email);
            return null;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - user is inactive: {Email}", request.Email);
            return null;
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            _logger.LogWarning("Login failed - invalid password for: {Email}", request.Email);
            return null;
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return user;
    }

    public async Task<bool> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User logged out: {UserId}", userId);
        return await Task.FromResult(true);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (!_passwordHasher.VerifyPassword(currentPassword, user.Password))
        {
            return false;
        }

        user.Password = _passwordHasher.HashPassword(newPassword);
        user.IsActive = true;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password changed for user: {UserId}", userId);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Password = _passwordHasher.HashPassword(newPassword);
        user.IsActive = true;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password reset for user: {UserId}", userId);
        return true;
    }
}
