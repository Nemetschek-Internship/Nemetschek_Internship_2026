namespace Entities.Models;

public class Parent
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public List<Student> Students { get; set; } = new List<Student>();
}
