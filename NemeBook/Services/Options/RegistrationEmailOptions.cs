namespace Services.Options;

public class RegistrationEmailOptions
{
    public string BaseUrl { get; set; } = null!;

    public string SetPasswordPath { get; set; } = "/Account/SetPassword";

    public string ParentSignUpPath { get; set; } = "/Account/ParentSignUp";

}
