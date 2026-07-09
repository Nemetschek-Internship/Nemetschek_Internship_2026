using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Новата парола е задължителна.")]
    [MinLength(8, ErrorMessage = "Новата парола трябва да бъде поне 8 символа.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Потвърждението на новата парола е задължително.")]
    [Compare("NewPassword", ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = null!;
}
