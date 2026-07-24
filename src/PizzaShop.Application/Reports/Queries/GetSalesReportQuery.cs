using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reports.Dtos;

namespace PizzaShop.Application.Reports.Queries;

/// <summary>
/// Admin sales report for orders placed in <c>[From, To]</c> (inclusive): order count, total
/// revenue and the top <see cref="TopItems"/> best-selling menu items by quantity sold.
/// </summary>
public sealed record GetSalesReportQuery(DateTimeOffset From, DateTimeOffset To, int TopItems = 5) : IQuery<SalesReportDto>;
