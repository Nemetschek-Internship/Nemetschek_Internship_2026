using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.Models;

public class Grade
{
    public Guid Id { get; set; }
    
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    
    public Guid ClassSubjectId { get; set; }
    public ClassSubject ClassSubject { get; set; } = null!;
    
    [Range(2, 6, ErrorMessage = "Оценката трябва да бъде между 2 и 6.")]
    public decimal Value { get; set; }

    public GradeType Type { get; set; }

    public string Note { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }
}
