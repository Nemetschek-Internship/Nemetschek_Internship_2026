namespace Entities.Models;

public class Class
{
    public Guid Id { get; set; }
    
    public int GradeNumber  { get; set; }
    
    public char Letter { get; set; }
    
    public Guid MainTeacherId { get; set; }
    public Teacher MainTeacher { get; set; } = null!;

    public List<Student> Students { get; set; } = new List<Student>();
    
    public List<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();

    public List<Event> Events { get; set; } = new List<Event>();
}
