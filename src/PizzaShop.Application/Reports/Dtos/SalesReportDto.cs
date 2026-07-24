using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Reports.Dtos;

/// <summary>Result of <c>GetSalesReportQuery</c> — admin sales report for a date range.</summary>
public sealed record SalesReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int OrderCount,
    MoneyDto Revenue,
    IReadOnlyList<TopMenuItemDto> TopMenuItems);
