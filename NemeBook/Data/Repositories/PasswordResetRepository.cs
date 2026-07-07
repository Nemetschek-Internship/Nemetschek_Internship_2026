using Data;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly NemeBookDbContext _dbContext;

    public PasswordResetRepository(NemeBookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateOrReplaceAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        var existingTokens = _dbContext.PasswordResetTokens.Where(t => t.UserId == token.UserId);
        _dbContext.PasswordResetTokens.RemoveRange(existingTokens);

        await _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.PasswordResetTokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (token is null) return;
        _dbContext.PasswordResetTokens.Remove(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = _dbContext.PasswordResetTokens.Where(t => t.UserId == userId);
        _dbContext.PasswordResetTokens.RemoveRange(tokens);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
