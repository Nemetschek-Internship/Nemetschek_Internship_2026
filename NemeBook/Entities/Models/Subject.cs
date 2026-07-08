namespace Entities.Models;

public class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public List<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();

    public List<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
