using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="ILoyaltyAccountRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryLoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly ConcurrentDictionary<Guid, LoyaltyAccount> _accounts = new();

    public Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.Values.FirstOrDefault(a => a.CustomerId == customerId));

    public Task AddAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyAccount account, CancellationToken cancellationToken)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }
}
