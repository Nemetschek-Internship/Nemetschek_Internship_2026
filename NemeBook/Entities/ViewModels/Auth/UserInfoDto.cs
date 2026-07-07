namespace Web.ViewModels.Auth;

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string MiddleName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = null!;

    public string FullName => $"{FirstName} {MiddleName} {LastName}";
}