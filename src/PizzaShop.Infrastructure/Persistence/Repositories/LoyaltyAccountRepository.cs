using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILoyaltyAccountRepository"/> — one account per
/// registered customer (ADR-0009).
/// </summary>
public sealed class LoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly PizzaShopDbContext _context;

    public LoyaltyAccountRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerId == customerId, cancellationToken);

    public Task AddAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        _context.LoyaltyAccounts.Add(account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        _context.LoyaltyAccounts.Update(account);
        return Task.CompletedTask;
    }
}
