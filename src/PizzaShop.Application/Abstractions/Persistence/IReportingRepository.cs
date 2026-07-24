namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Read-only aggregate queries over the <c>Order</c> aggregate for admin reporting
/// (application-layer.md 3.1 pattern, new Reports module). Deliberately separate from
/// <see cref="IOrderRepository"/> — this port never loads/persists an <c>Order</c> aggregate,
/// only pre-aggregated figures for the sales report.
/// </summary>
public interface IReportingRepository
{
    /// <summary>
    /// Aggregates orders placed in <c>[from, to]</c> (inclusive), excluding cancelled/rejected
    /// orders, into order count, total revenue and the top-selling menu items by quantity.
    /// </summary>
    Task<SalesReportData> GetSalesReportAsync(DateTimeOffset from, DateTimeOffset to, int topItems, CancellationToken cancellationToken);
}
