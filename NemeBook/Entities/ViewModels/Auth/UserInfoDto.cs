namespace Entities.ViewModels.Auth;

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = null!;

    public string FullName => string.Join(
        " ",
        new[] { FirstName, MiddleName, LastName }
            .Where(name => !string.IsNullOrWhiteSpace(name)));
}
