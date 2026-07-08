using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.Models;

public class User
{
    public Guid Id { get; set; }
    
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Невалиден формат на имейл адрес.")]
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;
    
    [Phone(ErrorMessage = "Невалиден формат на телефонен номер.")]
    public string? PhoneNumber { get; set; }
    
    public bool IsDeleted { get; set; }

    public UserRole Role { get; set; }

    public Student? Student { get; set; }
    public Parent? Parent { get; set; }
    public Teacher? Teacher { get; set; }
    
    public PasswordResetToken? PasswordResetToken { get; set; }

    public List<Chat> Chats { get; set; } = new List<Chat>();
    public List<Notification> Notifications { get; set; } = new List<Notification>();
}
