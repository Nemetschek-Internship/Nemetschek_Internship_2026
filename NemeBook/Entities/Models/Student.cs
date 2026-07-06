namespace Entities.Models;

public class Student
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateOnly BirthDate { get; set; }
    
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public List<Parent> Parents { get; set; } = new List<Parent>();
}
