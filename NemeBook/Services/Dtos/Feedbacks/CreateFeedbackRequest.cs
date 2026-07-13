using System.ComponentModel.DataAnnotations;
using Entities.Enums;

namespace Services.Dtos.Feedbacks;

public class CreateFeedbackRequest
{
    [Required(ErrorMessage = "Изберете ученик.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Изберете предмет за класа.")]
    public Guid ClassSubjectId { get; set; }

    public Guid? ClassScheduleEntryId { get; set; }

    [Required(ErrorMessage = "Изберете дата.")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Изберете тип.")]
    public FeedbackType Type { get; set; }

    [Required(ErrorMessage = "Описанието е задължително.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Описанието трябва да е между 3 и 1000 символа.")]
    public string Description { get; set; } = string.Empty;

    // Add the properties needed for creating feedback
    // For example:
    // public Guid StudentId { get; set; }
    // public string Content { get; set; }
    // public int Rating { get; set; }
}