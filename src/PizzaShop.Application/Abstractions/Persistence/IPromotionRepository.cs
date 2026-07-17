using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Promotion"/> aggregate (application-layer.md 3.1).
/// </summary>
public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Looks up a promotion by its coupon code (case-insensitive, ADR-none — see <see cref="Promotion"/>).</summary>
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Active promotions with no coupon code — applied automatically without the customer
    /// supplying anything (as opposed to code-gated promotions, application-layer.md 4.5).
    /// </summary>
    Task<IReadOnlyList<Promotion>> GetActiveAutomaticAsync(CancellationToken cancellationToken);

    /// <summary>All promotions, for the management list (<c>GetPromotionsQuery</c>, RestaurantAdmin).</summary>
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);

    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken);
}
