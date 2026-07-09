using Entities.Models;
using Services.Interfaces;
using Services.Repositories;

namespace Services.Services.Accounts;

public class AccountService : IAccountService
{
    private readonly IAccountsRepository _accountsRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IEmailService _emailService;

    public AccountService(
        IAccountsRepository accountsRepository,
        IPasswordResetRepository passwordResetRepository,
        IEmailService emailService)
    {
        _accountsRepository = accountsRepository;
        _passwordResetRepository = passwordResetRepository;
        _emailService = emailService;
    }

    public async Task SendPasswordResetAsync(string email, string resetUrlBase, CancellationToken cancellationToken = default)
    {
        var user = await _accountsRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null) return; 

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
    }

}
