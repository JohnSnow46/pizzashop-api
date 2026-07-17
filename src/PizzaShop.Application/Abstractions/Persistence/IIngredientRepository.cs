using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Repository for the <see cref="Ingredient"/> dictionary (application-layer.md 3.1).
/// </summary>
public interface IIngredientRepository
{
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ingredient>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);

    Task UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken);
}
