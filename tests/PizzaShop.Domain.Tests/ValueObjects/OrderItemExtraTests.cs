using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class OrderItemExtraTests
{
    [Fact]
    public void Constructor_EmptyIngredientId_ThrowsArgumentException()
    {
        var act = () => new OrderItemExtra(Guid.Empty, "Cheese", new Money(2m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_MissingName_ThrowsArgumentException()
    {
        var act = () => new OrderItemExtra(Guid.NewGuid(), "", new Money(2m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = new OrderItemExtra(id, "Cheese", new Money(2m));
        var b = new OrderItemExtra(id, "Cheese", new Money(2m));

        (a == b).Should().BeTrue();
    }
}
