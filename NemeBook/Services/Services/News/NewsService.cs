using Services.Interfaces.News;
using Services.Repositories;
using NewsEntity = Entities.Models.News;

namespace Services.Services.News;

public class NewsService : INewsService
{
    private readonly INewsRepository newsRepository;

    public NewsService(INewsRepository newsRepository)
    {
        this.newsRepository = newsRepository;
    }

    public async Task<IReadOnlyList<NewsEntity>> GetAllNewsAsync(CancellationToken cancellationToken = default)
    {
        return (await newsRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(news => news.CreatedAt)
            .ThenByDescending(news => news.Id)
            .ToList();
    }

    public async Task CreateNewsAsync(
        string title,
        string text,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var (normalizedTitle, normalizedText) = NormalizeNewsContent(title, text);

        var news = new NewsEntity
        {
            Id = Guid.NewGuid(),
            Title = normalizedTitle,
            Text = normalizedText,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
        };

        await newsRepository.CreateAsync(news, cancellationToken);
    }

    public async Task UpdateNewsAsync(
        Guid id,
        string title,
        string text,
        CancellationToken cancellationToken = default)
    {
        var news = await newsRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new InvalidOperationException("Новината не беше намерена.");

        var (normalizedTitle, normalizedText) = NormalizeNewsContent(title, text);

        news.Title = normalizedTitle;
        news.Text = normalizedText;

        await newsRepository.UpdateAsync(news, cancellationToken);
    }

    public Task DeleteNewsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return newsRepository.DeleteAsync(id, cancellationToken);
    }

    private static (string Title, string Text) NormalizeNewsContent(string title, string text)
    {
        var normalizedTitle = title.Trim();
        var normalizedText = text.Trim();

        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new ArgumentException("Заглавието е задължително.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            throw new ArgumentException("Текстът е задължителен.", nameof(text));
        }

        return (normalizedTitle, normalizedText);
    }
}
