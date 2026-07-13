using Entities.Models;

namespace Services.Repositories;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Feedback>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Feedback>> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Feedback>> GetByClassAsync(
        Guid classId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Feedback feedback, CancellationToken cancellationToken = default);

    Task UpdateAsync(Feedback feedback, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
