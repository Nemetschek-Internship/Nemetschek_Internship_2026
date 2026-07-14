using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.ViewModels.Grades;

public class CreateGradeRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid ClassSubjectId { get; set; }

    [Range(2, 6, ErrorMessage = "Оценката трябва да бъде между 2 и 6.")]
    public decimal Value { get; set; }

    [Required]
    public GradeType Type { get; set; }

    public string Note { get; set; } = string.Empty;
}
