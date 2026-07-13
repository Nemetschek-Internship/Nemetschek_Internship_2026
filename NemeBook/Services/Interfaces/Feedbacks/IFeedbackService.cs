using Services.Dtos.Feedbacks;

namespace Services.Interfaces.Feedbacks;

public interface IFeedbackService
{
    Task<Guid> CreateAsync(
        CreateFeedbackRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentFeedbackViewModel> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<ClassFeedbackViewModel> GetByClassAsync(
        Guid classId,
        CancellationToken cancellationToken = default);
}