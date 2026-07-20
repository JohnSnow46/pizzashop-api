using PizzaShop.Application.Common.Abstractions;

namespace PizzaShop.Infrastructure.Persistence;

/// <summary>
/// Thin wrapper around <see cref="PizzaShopDbContext.SaveChangesAsync"/> — the transactional
/// boundary shared with the repositories, which never commit on their own
/// (infrastructure-layer.md 4.2).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PizzaShopDbContext _context;

    public UnitOfWork(PizzaShopDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await _context.SaveChangesAsync(cancellationToken);
}
