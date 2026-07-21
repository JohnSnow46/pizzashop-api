using System.Collections.Concurrent;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>In-memory <see cref="IIngredientRepository"/> — see <see cref="InMemoryUserAccountRepository"/> for rationale.</summary>
public sealed class InMemoryIngredientRepository : IIngredientRepository
{
    private readonly ConcurrentDictionary<Guid, Ingredient> _ingredients = new();

    public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_ingredients.TryGetValue(id, out var ingredient) ? ingredient : null);

    public Task<IReadOnlyList<Ingredient>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Ingredient>>(_ingredients.Values.Where(i => ids.Contains(i.Id)).ToList());

    public Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Ingredient>>(_ingredients.Values.ToList());

    public Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        _ingredients[ingredient.Id] = ingredient;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        _ingredients[ingredient.Id] = ingredient;
        return Task.CompletedTask;
    }
}
