using FluentAssertions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Catalog;

public class MenuItemVariantTests
{
    [Fact]
    public void Create_MissingName_ThrowsArgumentException()
    {
        var act = () => MenuItemVariant.Create("", new Money(10m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_DefaultsIsDefaultToFalse()
    {
        var variant = MenuItemVariant.Create("Small", new Money(10m));

        variant.IsDefault.Should().BeFalse();
    }

    // UpdatePrice/Rename/MarkDefault are internal — mutated only by MenuItem (the aggregate
    // root); covered indirectly via MenuItemTests (UpdateVariantPrice, RenameVariant,
    // SetDefaultVariant, RemoveVariant).
}
