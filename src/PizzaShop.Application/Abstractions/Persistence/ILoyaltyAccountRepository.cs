using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="LoyaltyAccount"/> aggregate (application-layer.md 3.1,
/// ADR-0009). One account per registered customer — guests never have one (ADR-0005).
/// </summary>
public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task AddAsync(LoyaltyAccount account, CancellationToken cancellationToken);

    Task UpdateAsync(LoyaltyAccount account, CancellationToken cancellationToken);
}
