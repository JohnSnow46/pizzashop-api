using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Identity;
using PizzaShop.Application.Identity.Abstractions;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IUserAccountRepository"/> (ADR-0026).</summary>
public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly PizzaShopDbContext _context;

    public UserAccountRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.UserAccounts.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.UserAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken) =>
        _context.UserAccounts.AnyAsync(a => a.Email == email, cancellationToken);

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _context.UserAccounts.Add(account);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.UserAccounts
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
}
