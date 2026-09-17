using FluentAssertions;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Promotions;

public class OrderDiscountLineTests
{
    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        var menuItemId = Guid.NewGuid();
        var unitPrice = new Money(10m);

        var line = new OrderDiscountLine(menuItemId, unitPrice, 2);

        line.MenuItemId.Should().Be(menuItemId);
        line.UnitPrice.Should().Be(unitPrice);
        line.Quantity.Should().Be(2);
    }

    [Fact]
    public void Constructor_EmptyMenuItemId_ThrowsArgumentException()
    {
        var act = () => new OrderDiscountLine(Guid.Empty, new Money(10m), 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullUnitPrice_ThrowsArgumentNullException()
    {
        var act = () => new OrderDiscountLine(Guid.NewGuid(), null!, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_QuantityLessThanOne_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var act = () => new OrderDiscountLine(Guid.NewGuid(), new Money(10m), quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_QuantityAtOne_DoesNotThrow()
    {
        var act = () => new OrderDiscountLine(Guid.NewGuid(), new Money(10m), 1);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public void Constructor_QuantityGreaterThanOneHundred_ThrowsArgumentOutOfRangeException(int quantity)
    {
        var act = () => new OrderDiscountLine(Guid.NewGuid(), new Money(10m), quantity);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_QuantityAtOneHundred_DoesNotThrow()
    {
        var act = () => new OrderDiscountLine(Guid.NewGuid(), new Money(10m), 100);

        act.Should().NotThrow();
    }
}
