using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="IPromotionRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryPromotionRepository : IPromotionRepository
{
    private readonly ConcurrentDictionary<Guid, Promotion> _promotions = new();

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_promotions.TryGetValue(id, out var promotion) ? promotion : null);

    public Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_promotions.Values.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Promotion>> GetActiveAutomaticAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(_promotions.Values.Where(p => p.IsActive && p.Code is null).ToList());

    public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Promotion>>(_promotions.Values.ToList());

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _promotions[promotion.Id] = promotion;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _promotions[promotion.Id] = promotion;
        return Task.CompletedTask;
    }
}
