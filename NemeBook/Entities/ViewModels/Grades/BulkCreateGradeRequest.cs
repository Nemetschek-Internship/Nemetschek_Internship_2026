using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Entities.ViewModels.Grades;

public class BulkCreateGradeRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Трябва да бъде избран поне един ученик.")]
    public List<Guid> StudentIds { get; set; } = new();

    [Required]
    public Guid ClassSubjectId { get; set; }

    [Range(2, 6, ErrorMessage = "Оценката трябва да бъде между 2 и 6.")]
    public decimal Value { get; set; }

    [Required]
    public GradeType Type { get; set; }

    public string Note { get; set; } = string.Empty;
}
