using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="IReviewRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryReviewRepository : IReviewRepository
{
    private readonly ConcurrentDictionary<Guid, Review> _reviews = new();

    public Task<Review?> GetByOrderAndMenuItemAsync(Guid orderId, Guid menuItemId, CancellationToken cancellationToken) =>
        Task.FromResult(_reviews.Values.FirstOrDefault(r => r.OrderId == orderId && r.MenuItemId == menuItemId));

    public Task<IReadOnlyList<Review>> GetByMenuItemIdAsync(Guid menuItemId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Review>>(
            _reviews.Values.Where(r => r.MenuItemId == menuItemId).OrderByDescending(r => r.CreatedAt).ToList());

    public Task<IReadOnlyList<Review>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Review>>(
            _reviews.Values.Where(r => r.CustomerId == customerId).OrderByDescending(r => r.CreatedAt).ToList());

    public Task<IReadOnlyDictionary<Guid, (double Average, int Count)>> GetRatingSummariesAsync(
        IEnumerable<Guid> menuItemIds, CancellationToken cancellationToken)
    {
        var ids = menuItemIds.ToHashSet();
        var summaries = _reviews.Values
            .Where(r => ids.Contains(r.MenuItemId))
            .GroupBy(r => r.MenuItemId)
            .ToDictionary(g => g.Key, g => (g.Average(r => (double)r.Rating), g.Count()));

        return Task.FromResult<IReadOnlyDictionary<Guid, (double Average, int Count)>>(summaries);
    }

    public Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        _reviews[review.Id] = review;
        return Task.CompletedTask;
    }
}
