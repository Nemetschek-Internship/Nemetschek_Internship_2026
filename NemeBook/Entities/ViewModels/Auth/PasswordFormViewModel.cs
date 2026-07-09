using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class PasswordFormViewModel
{
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [MinLength(8, ErrorMessage = "Паролата трябва да бъде поне 8 символа.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Потвърждението на паролата е задължително.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
