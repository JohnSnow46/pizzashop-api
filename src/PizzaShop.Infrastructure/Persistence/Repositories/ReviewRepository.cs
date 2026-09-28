using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReviewRepository"/> (ADR-0042, ADR-0020).
/// </summary>
public sealed class ReviewRepository : IReviewRepository
{
    private readonly PizzaShopDbContext _context;

    public ReviewRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<Review?> GetByOrderAndMenuItemAsync(Guid orderId, Guid menuItemId, CancellationToken cancellationToken) =>
        _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == orderId && r.MenuItemId == menuItemId, cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByMenuItemIdAsync(Guid menuItemId, CancellationToken cancellationToken) =>
        await _context.Reviews
            .Where(r => r.MenuItemId == menuItemId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Reviews
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        _context.Reviews.Add(review);
        return Task.CompletedTask;
    }
}
