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

    public async Task<IReadOnlyDictionary<Guid, (double Average, int Count)>> GetRatingSummariesAsync(
        IEnumerable<Guid> menuItemIds, CancellationToken cancellationToken)
    {
        var ids = menuItemIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, (double Average, int Count)>();
        }

        var summaries = await _context.Reviews
            .Where(r => ids.Contains(r.MenuItemId))
            .GroupBy(r => r.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return summaries.ToDictionary(s => s.MenuItemId, s => (s.Average, s.Count));
    }

    public Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        _context.Reviews.Add(review);
        return Task.CompletedTask;
    }
}
