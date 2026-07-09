namespace Services.Interfaces;

public interface IAccountService
{
    /// <summary>
    /// Generates a password reset token, stores it, and sends a reset email to the user.
    /// The `resetUrlBase` should be a full URL prefix (e.g. https://app.example.com/reset-password) to which the token will be appended as `?token=...`.
    /// </summary>
    Task<bool> SendPasswordResetAsync(string email, string resetUrlBase, CancellationToken cancellationToken = default);

    Task<bool> IsPasswordResetTokenValidAsync(string token, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default);
}
