using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPasswordResetTokenRepository"/>.</summary>
public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly PizzaShopDbContext _context;

    public PasswordResetTokenRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveByUserAccountIdAsync(Guid userAccountId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await _context.PasswordResetTokens
            .Where(t => t.UserAccountId == userAccountId && t.UsedAt == null && t.ExpiresAt >= now)
            .ToListAsync(cancellationToken);

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        _context.PasswordResetTokens.Add(token);
        return Task.CompletedTask;
    }
}
