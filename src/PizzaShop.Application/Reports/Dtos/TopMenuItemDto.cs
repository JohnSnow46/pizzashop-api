using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Reports.Dtos;

/// <summary>One row of the "top selling menu items" table in <see cref="SalesReportDto"/>.</summary>
public sealed record TopMenuItemDto(Guid MenuItemId, string MenuItemName, int QuantitySold, MoneyDto Revenue);
