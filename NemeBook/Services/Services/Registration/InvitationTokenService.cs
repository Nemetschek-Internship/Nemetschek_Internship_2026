using System.Security.Cryptography;
using System.Text;
using Services.Interfaces.Registration;

namespace Services.Services.Registration;

public class InvitationTokenService : IInvitationTokenService
{
    public string GenerateToken()
    {
        return ToBase64Url(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Токенът не може да бъде празен.", nameof(token));
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
