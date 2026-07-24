using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Verifies <see cref="ReportingRepository"/>'s aggregation queries actually translate to SQL
/// against a real PostgreSQL provider (Money value-converter member access, GroupBy over the
/// owned <c>OrderItems</c> collection) rather than throwing/falling back to client evaluation.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ReportingRepositoryTests : PostgresRepositoryTestBase
{
    public ReportingRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetSalesReportAsync_OrdersInRange_AggregatesCountRevenueAndTopItems()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();
        var deliveryOrder = DomainTestFactory.CreateDeliveryOrderWithExtras(restaurant, Guid.NewGuid());
        var pickupOrder = DomainTestFactory.CreatePickupOrder(restaurant, "20260720-0002");

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Restaurants.AddAsync(restaurant);
            await writeContext.Orders.AddAsync(deliveryOrder);
            await writeContext.Orders.AddAsync(pickupOrder);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var repository = new ReportingRepository(readContext);

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        var report = await repository.GetSalesReportAsync(from, to, topItems: 5, CancellationToken.None);

        report.OrderCount.Should().Be(2);
        report.Revenue.Amount.Should().Be(deliveryOrder.Total.Amount + pickupOrder.Total.Amount);

        // Each order's single line uses a distinct (randomly generated) MenuItemId, even
        // though both happen to be named "Margherita" — the grouping key is MenuItemId, so
        // this yields two separate top-item rows, not one merged row.
        report.TopMenuItems.Should().HaveCount(2);
        report.TopMenuItems.Should().OnlyContain(item => item.MenuItemName == "Margherita");
        report.TopMenuItems.Select(item => item.QuantitySold).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetSalesReportAsync_OrderOutsideRange_IsExcluded()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();
        var order = DomainTestFactory.CreatePickupOrder(restaurant);

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Restaurants.AddAsync(restaurant);
            await writeContext.Orders.AddAsync(order);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var repository = new ReportingRepository(readContext);

        var from = DateTimeOffset.UtcNow.AddDays(-10);
        var to = DateTimeOffset.UtcNow.AddDays(-5);

        var report = await repository.GetSalesReportAsync(from, to, topItems: 5, CancellationToken.None);

        report.OrderCount.Should().Be(0);
        report.Revenue.Amount.Should().Be(0m);
        report.TopMenuItems.Should().BeEmpty();
    }
}
