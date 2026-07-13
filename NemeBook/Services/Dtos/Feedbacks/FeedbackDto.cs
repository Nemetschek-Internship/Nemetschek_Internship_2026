using Entities.Enums;

namespace Services.Dtos.Feedbacks;

public class FeedbackDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public FeedbackType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class StudentFeedbackViewModel
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public IReadOnlyList<FeedbackDto> Items { get; set; } = Array.Empty<FeedbackDto>();
}

public class ClassFeedbackViewModel
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public IReadOnlyList<FeedbackDto> Items { get; set; } = Array.Empty<FeedbackDto>();
}