using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class ParentSignUpViewModel
{
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters.")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Фамилията е задължителна.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone number is invalid.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [MinLength(8, ErrorMessage = "Паролата трябва да бъде поне 8 символа.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
