using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>Raw aggregation result returned by <see cref="IReportingRepository.GetSalesReportAsync"/>.</summary>
public sealed record SalesReportData(int OrderCount, Money Revenue, IReadOnlyList<TopMenuItemSales> TopMenuItems);

/// <summary>Per-menu-item sales aggregate, part of <see cref="SalesReportData"/>.</summary>
public sealed record TopMenuItemSales(Guid MenuItemId, string MenuItemName, int QuantitySold, Money Revenue);
