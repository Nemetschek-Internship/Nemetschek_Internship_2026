using NewsEntity = Entities.Models.News;

namespace Services.Interfaces.News;

public interface INewsService
{
    Task<IReadOnlyList<NewsEntity>> GetAllNewsAsync(CancellationToken cancellationToken = default);

    Task CreateNewsAsync(
        string title,
        string text,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    Task UpdateNewsAsync(
        Guid id,
        string title,
        string text,
        CancellationToken cancellationToken = default);

    Task DeleteNewsAsync(Guid id, CancellationToken cancellationToken = default);
}
