using System.ComponentModel.DataAnnotations;

namespace Entities.ViewModels.Auth;

public class ParentSignUpViewModel
{
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа.")]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Презимето не може да бъде по-дълго от 100 символа.")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Фамилията е задължителна.")]
    [StringLength(100, ErrorMessage = "Фамилията не може да бъде по-дълга от 100 символа.")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Телефонният номер е невалиден.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [MinLength(8, ErrorMessage = "Паролата трябва да бъде поне 8 символа.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Потвърждението на паролата е задължително.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
