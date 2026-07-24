using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReportingRepository"/>. Runs two queries rather than
/// one, because the order-level aggregate (revenue) and the per-menu-item aggregate (top items)
/// group by different keys and would otherwise force a cross join. Neither query can push its
/// final <c>Sum</c>/<c>GroupBy</c> down to SQL: <c>Money</c> is mapped via a
/// <see cref="Converters.MoneyConverter"/> onto a single <c>numeric</c> column, and member
/// access on a value-converted property (<c>o.Total.Amount</c>, <c>i.LineTotal.Amount</c>) is
/// not translatable by EF Core/Npgsql — confirmed by <c>ReportingRepositoryTests</c>
/// (Testcontainers), which throws <c>InvalidOperationException</c> for that shape. Each query
/// therefore projects the plain rows needed (still filtered/joined in SQL) and aggregates them
/// in memory, which is precise for this single-restaurant portfolio scope.
/// </summary>
public sealed class ReportingRepository : IReportingRepository
{
    private static readonly OrderStatus[] ExcludedStatuses =
    {
        OrderStatus.Cancelled,
        OrderStatus.Rejected,
    };

    private readonly PizzaShopDbContext _context;

    public ReportingRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportData> GetSalesReportAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int topItems,
        CancellationToken cancellationToken)
    {
        var ordersInRange = _context.Orders
            .Where(o => o.PlacedAt >= from && o.PlacedAt <= to)
            .Where(o => !ExcludedStatuses.Contains(o.Status));

        // SQL does the filtering/projection; only the resulting Money values are aggregated
        // client-side (member access on a value-converted property does not translate, see
        // class remarks above).
        var orderTotals = await ordersInRange.Select(o => o.Total).ToListAsync(cancellationToken);
        var orderCount = orderTotals.Count;
        var revenue = orderTotals.Aggregate(Money.Zero(), (sum, total) => sum.Add(total));

        var lineItems = await ordersInRange
            .SelectMany(o => o.Items)
            .Select(i => new { i.MenuItemId, i.MenuItemName, i.Quantity, i.LineTotal })
            .ToListAsync(cancellationToken);

        var topMenuItems = lineItems
            .GroupBy(i => (i.MenuItemId, i.MenuItemName))
            .Select(g => new TopMenuItemSales(
                g.Key.MenuItemId,
                g.Key.MenuItemName,
                g.Sum(i => i.Quantity),
                g.Aggregate(Money.Zero(), (sum, i) => sum.Add(i.LineTotal))))
            .OrderByDescending(item => item.QuantitySold)
            .Take(topItems)
            .ToList();

        return new SalesReportData(orderCount, revenue, topMenuItems);
    }
}
