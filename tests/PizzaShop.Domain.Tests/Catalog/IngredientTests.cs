using FluentAssertions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Catalog;

public class IngredientTests
{
    [Fact]
    public void Create_MissingName_ThrowsArgumentException()
    {
        var act = () => Ingredient.Create("", new Money(1m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ValidArguments_IsAvailableByDefault()
    {
        var ingredient = Ingredient.Create("Cheese", new Money(2m));

        ingredient.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void MarkUnavailable_SetsIsAvailableFalse()
    {
        var ingredient = Ingredient.Create("Cheese", new Money(2m));

        ingredient.MarkUnavailable();

        ingredient.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void UpdatePrice_ChangesExtraPrice()
    {
        var ingredient = Ingredient.Create("Cheese", new Money(2m));

        ingredient.UpdatePrice(new Money(5m));

        ingredient.ExtraPrice.Amount.Should().Be(5m);
    }
}
