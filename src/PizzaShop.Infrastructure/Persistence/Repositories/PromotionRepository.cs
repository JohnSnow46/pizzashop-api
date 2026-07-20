using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPromotionRepository"/> (ADR-0019, ADR-0020).
/// </summary>
public sealed class PromotionRepository : IPromotionRepository
{
    private readonly PizzaShopDbContext _context;

    public PromotionRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Promotions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _context.Promotions.FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAutomaticAsync(CancellationToken cancellationToken) =>
        await _context.Promotions.Where(p => p.IsActive && p.Code == null).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Promotions.ToListAsync(cancellationToken);

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _context.Promotions.Add(promotion);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _context.Promotions.Update(promotion);
        return Task.CompletedTask;
    }
}
