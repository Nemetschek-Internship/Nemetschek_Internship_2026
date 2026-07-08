using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден формат на имейл адрес.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [MinLength(6, ErrorMessage = "Паролата трябва да бъде поне 6 символа.")]
    public string Password { get; set; } = null!;

    public bool RememberMe { get; set; }
}
