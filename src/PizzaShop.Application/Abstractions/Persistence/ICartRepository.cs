using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Cart"/> aggregate (ADR-0043). One cart per customer.
/// </summary>
public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task AddAsync(Cart cart, CancellationToken cancellationToken);
}
