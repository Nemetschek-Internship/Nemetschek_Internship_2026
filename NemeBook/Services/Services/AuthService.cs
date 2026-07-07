// Services/Services/AuthService.cs
using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Auth;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Repositories;

namespace Services.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
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

        if (!VerifyPassword(request.Password, user.Password))
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

        if (!VerifyPassword(currentPassword, user.Password))
        {
            return false;
        }

        user.Password = HashPassword(newPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password changed for user: {UserId}", userId);
        return true;
    }

    private static string HashPassword(string password)
    {
        return password;
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        
        return password == hashedPassword;
    }
}