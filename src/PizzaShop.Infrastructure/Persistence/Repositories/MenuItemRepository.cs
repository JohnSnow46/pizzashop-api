using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMenuItemRepository"/>. <c>Variants</c> is owned and
/// always included automatically; <c>BaseIngredients</c>/<c>AllowedExtras</c> are skip
/// navigations (many-to-many to the shared <see cref="Ingredient"/> dictionary) and need an
/// explicit <see cref="EntityFrameworkQueryableExtensions.Include{TEntity}"/> (ADR-0020,
/// infrastructure-layer.md 2.3).
/// </summary>
public sealed class MenuItemRepository : IMenuItemRepository
{
    private readonly PizzaShopDbContext _context;

    public MenuItemRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    private IQueryable<MenuItem> QueryWithIngredients() =>
        _context.MenuItems.Include(m => m.BaseIngredients).Include(m => m.AllowedExtras);

    public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        QueryWithIngredients().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MenuItem>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        return await QueryWithIngredients().Where(m => idList.Contains(m.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuItem>> GetMenuAsync(CancellationToken cancellationToken) =>
        await QueryWithIngredients().Where(m => m.IsAvailable).ToListAsync(cancellationToken);

    public Task AddAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _context.MenuItems.Add(menuItem);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(MenuItem menuItem, CancellationToken cancellationToken)
    {
        _context.MenuItems.Update(menuItem);
        return Task.CompletedTask;
    }
}
