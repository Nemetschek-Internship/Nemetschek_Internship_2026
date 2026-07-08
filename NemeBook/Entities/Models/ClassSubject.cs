namespace Entities.Models;

public class ClassSubject
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public List<Grade> Grades { get; set; } = new List<Grade>();
    public List<Absence> Absences { get; set; } = new List<Absence>();
    public List<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public List<ClassScheduleEntry> ScheduleEntries { get; set; } = new List<ClassScheduleEntry>();
    public List<Event> Events { get; set; } = new List<Event>();
}
