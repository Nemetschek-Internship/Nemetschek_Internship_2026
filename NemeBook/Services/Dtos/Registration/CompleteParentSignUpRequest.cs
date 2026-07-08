namespace Services.Dtos.Registration;

public class CompleteParentSignUpRequest
{
    public string Token { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string Password { get; set; } = null!;
}
