using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Reports.Queries;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Reports.Queries;

public class GetSalesReportQueryHandlerTests
{
    private readonly Mock<IReportingRepository> _reportingRepository = new();

    private GetSalesReportQueryHandler CreateHandler() => new(_reportingRepository.Object);

    [Fact]
    public async Task Handle_RepositoryReturnsAggregates_MapsToSalesReportDto()
    {
        var from = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 7, 31, 23, 59, 59, TimeSpan.Zero);
        var menuItemId = Guid.NewGuid();

        var data = new SalesReportData(
            OrderCount: 12,
            Revenue: new Money(456.78m),
            TopMenuItems: new List<TopMenuItemSales>
            {
                new(menuItemId, "Margherita", 20, new Money(300m)),
            });

        _reportingRepository
            .Setup(r => r.GetSalesReportAsync(from, to, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetSalesReportQuery(from, to, 5), CancellationToken.None);

        result.From.Should().Be(from);
        result.To.Should().Be(to);
        result.OrderCount.Should().Be(12);
        result.Revenue.Amount.Should().Be(456.78m);
        result.TopMenuItems.Should().ContainSingle();
        result.TopMenuItems[0].MenuItemId.Should().Be(menuItemId);
        result.TopMenuItems[0].MenuItemName.Should().Be("Margherita");
        result.TopMenuItems[0].QuantitySold.Should().Be(20);
        result.TopMenuItems[0].Revenue.Amount.Should().Be(300m);
    }
}
