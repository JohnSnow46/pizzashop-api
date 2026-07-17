using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for <see cref="MenuItem"/> (application-layer.md 3.1).
/// </summary>
public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves several menu items by id at once (e.g. building a cart/order).
    /// </summary>
    Task<IReadOnlyList<MenuItem>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Available (<see cref="MenuItem.IsAvailable"/>) items to show in the public menu.
    /// </summary>
    Task<IReadOnlyList<MenuItem>> GetMenuAsync(CancellationToken cancellationToken);

    Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken);

    Task UpdateAsync(MenuItem menuItem, CancellationToken cancellationToken);
}
