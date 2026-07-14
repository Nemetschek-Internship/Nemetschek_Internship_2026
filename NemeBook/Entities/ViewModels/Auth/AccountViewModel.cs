namespace Entities.ViewModels.Auth;

public class AccountViewModel
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public string FullName => string.Join(
        " ",
        new[] { FirstName, MiddleName, LastName }
            .Where(name => !string.IsNullOrWhiteSpace(name)));

    public string Initials
    {
        get
        {
            var firstInitial = string.IsNullOrWhiteSpace(FirstName) ? string.Empty : FirstName[0].ToString();
            var lastInitial = string.IsNullOrWhiteSpace(LastName) ? string.Empty : LastName[0].ToString();

            return $"{firstInitial}{lastInitial}".ToUpperInvariant();
        }
    }
}
