namespace PizzaShop.Application.Common.Abstractions;

/// <summary>
/// Transactional boundary for a use case (ADR-0015 — e.g. Order + Promotion.RecordUsage +
/// LoyaltyAccount.Redeem committed together in later iterations).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
