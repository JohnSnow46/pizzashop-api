using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Review"/> aggregate (ADR-0042).
/// </summary>
public interface IReviewRepository
{
    /// <summary>Used by <c>CreateReviewCommandHandler</c> to enforce one review per (OrderId, MenuItemId).</summary>
    Task<Review?> GetByOrderAndMenuItemAsync(Guid orderId, Guid menuItemId, CancellationToken cancellationToken);

    /// <summary>All reviews for a menu item, for the public catalog view.</summary>
    Task<IReadOnlyList<Review>> GetByMenuItemIdAsync(Guid menuItemId, CancellationToken cancellationToken);

    /// <summary>A customer's own reviews.</summary>
    Task<IReadOnlyList<Review>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Average rating and review count per menu item, for the ones that have at least one review.
    /// Used to show an aggregate rating on the catalog (<c>MenuItemDto</c>) without loading every
    /// <see cref="Review"/> row.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, (double Average, int Count)>> GetRatingSummariesAsync(
        IEnumerable<Guid> menuItemIds, CancellationToken cancellationToken);

    Task AddAsync(Review review, CancellationToken cancellationToken);
}
