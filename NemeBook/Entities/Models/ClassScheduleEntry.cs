namespace Entities.Models;

public class ClassScheduleEntry
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;

    public Guid ClassSubjectId { get; set; }
    public ClassSubject ClassSubject { get; set; } = null!;

    public Guid? SubstituteTeacherId { get; set; }
    public Teacher? SubstituteTeacher { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public int PeriodNumber { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }

    public List<Absence> Absences { get; set; } = new List<Absence>();

    public List<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
