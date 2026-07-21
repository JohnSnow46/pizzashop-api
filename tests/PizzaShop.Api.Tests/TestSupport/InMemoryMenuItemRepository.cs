using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="IMenuItemRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryMenuItemRepository : IMenuItemRepository
{
    private readonly ConcurrentDictionary<Guid, MenuItem> _items = new();

    public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task<IReadOnlyList<MenuItem>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MenuItem>>(_items.Values.Where(i => ids.Contains(i.Id)).ToList());

    public Task<IReadOnlyList<MenuItem>> GetMenuAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MenuItem>>(_items.Values.Where(i => i.IsAvailable).ToList());

    public Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _items[menuItem.Id] = menuItem;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _items[menuItem.Id] = menuItem;
        return Task.CompletedTask;
    }
}
