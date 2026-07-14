using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class NewsRepository : INewsRepository
{
    private readonly NemeBookDbContext dbContext;

    public NewsRepository(NemeBookDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<News?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.News
            .Include(news => news.CreatedByUser)
            .FirstOrDefaultAsync(news => news.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<News>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.News
            .Include(news => news.CreatedByUser)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(News news, CancellationToken cancellationToken = default)
    {
        await dbContext.News.AddAsync(news, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(News news, CancellationToken cancellationToken = default)
    {
        dbContext.News.Update(news);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var news = await GetByIdAsync(id, cancellationToken);
        if (news is null)
        {
            return;
        }

        dbContext.News.Remove(news);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
