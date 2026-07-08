using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Текущата парола е задължителна.")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Новата парола е задължителна.")]
    [MinLength(6, ErrorMessage = "Новата парола трябва да бъде поне 6 символа.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Потвърждението на новата парола е задължително.")]
    [Compare("NewPassword", ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = null!;
}
