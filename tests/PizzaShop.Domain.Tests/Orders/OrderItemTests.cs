using FluentAssertions;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Orders;

public class OrderItemTests
{
    [Fact]
    public void Create_QuantityLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => OrderItem.Create(Guid.NewGuid(), "Margherita", new Money(25m), quantity: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_EmptyMenuItemId_ThrowsArgumentException()
    {
        var act = () => OrderItem.Create(Guid.Empty, "Margherita", new Money(25m), quantity: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LineTotal_NoExtras_EqualsUnitPriceTimesQuantity()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Margherita", new Money(25m), quantity: 2);

        item.LineTotal.Amount.Should().Be(50m);
    }

    [Fact]
    public void LineTotal_WithExtras_IncludesExtrasBeforeMultiplyingByQuantity()
    {
        var extras = new[]
        {
            new OrderItemExtra(Guid.NewGuid(), "Mushroom", new Money(3m)),
            new OrderItemExtra(Guid.NewGuid(), "Bacon", new Money(5m)),
        };

        var item = OrderItem.Create(Guid.NewGuid(), "Margherita", new Money(25m), quantity: 2, extras: extras);

        // (25 + 3 + 5) * 2 = 66
        item.LineTotal.Amount.Should().Be(66m);
    }
}
