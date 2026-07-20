using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IIngredientRepository"/> — the shared ingredient
/// dictionary (infrastructure-layer.md 2.3).
/// </summary>
public sealed class IngredientRepository : IIngredientRepository
{
    private readonly PizzaShopDbContext _context;

    public IngredientRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ingredient>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return await _context.Ingredients.Where(i => idList.Contains(i.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Ingredients.ToListAsync(cancellationToken);

    public Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        _context.Ingredients.Add(ingredient);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        _context.Ingredients.Update(ingredient);
        return Task.CompletedTask;
    }
}
