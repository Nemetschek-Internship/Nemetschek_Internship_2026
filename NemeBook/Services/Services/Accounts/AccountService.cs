using Entities.Models;
using Services.Interfaces;
using Services.Interfaces.Security;
using Services.Repositories;

namespace Services.Services.Accounts;

public class AccountService : IAccountService
{
    private readonly IAccountsRepository _accountsRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;

    public AccountService(
        IAccountsRepository accountsRepository,
        IPasswordResetRepository passwordResetRepository,
        IEmailService emailService,
        IPasswordHasher passwordHasher)
    {
        _accountsRepository = accountsRepository;
        _passwordResetRepository = passwordResetRepository;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> SendPasswordResetAsync(string email, string resetUrlBase, CancellationToken cancellationToken = default)
    {
        var user = await _accountsRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return false;
        }

        var token = new PasswordResetToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        };

        await _passwordResetRepository.CreateOrReplaceAsync(token, cancellationToken);

        var separator = resetUrlBase.Contains('?') ? "&" : "?";
        var resetLink = $"{resetUrlBase}{separator}token={Uri.EscapeDataString(token.Token)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink, cancellationToken);
        return true;
    }

    public async Task<bool> IsPasswordResetTokenValidAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var passwordResetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);

        return passwordResetToken is not null
            && passwordResetToken.User is not null
            && passwordResetToken.User.IsActive
            && passwordResetToken.ExpiresAt >= DateTime.UtcNow;
    }

    public async Task ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Password reset token is required.", nameof(token));
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters.", nameof(password));
        }

        var passwordResetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);
        if (passwordResetToken is null || passwordResetToken.User is null || !passwordResetToken.User.IsActive)
        {
            throw new InvalidOperationException("Password reset link is invalid.");
        }

        if (passwordResetToken.ExpiresAt < DateTime.UtcNow)
        {
            await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);
            throw new InvalidOperationException("Password reset link has expired.");
        }

        passwordResetToken.User.Password = _passwordHasher.HashPassword(password);
        passwordResetToken.User.IsActive = true;

        await _accountsRepository.UpdateAsync(passwordResetToken.User, cancellationToken);
        await _passwordResetRepository.DeleteAsync(passwordResetToken.Id, cancellationToken);
    }
}
