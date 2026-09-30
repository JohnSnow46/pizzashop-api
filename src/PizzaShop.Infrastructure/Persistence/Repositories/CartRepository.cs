using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICartRepository"/> (ADR-0043, ADR-0020).
/// </summary>
public sealed class CartRepository : ICartRepository
{
    private readonly PizzaShopDbContext _context;

    public CartRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

    public Task AddAsync(Cart cart, CancellationToken cancellationToken)
    {
        _context.Carts.Add(cart);
        return Task.CompletedTask;
    }
}
