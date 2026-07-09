using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден формат на имейл адрес.")]
    public string Email { get; set; } = string.Empty;
}
