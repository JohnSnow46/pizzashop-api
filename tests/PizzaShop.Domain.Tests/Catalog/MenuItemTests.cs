using FluentAssertions;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Catalog;

public class MenuItemTests
{
    private static Ingredient Mozzarella() => Ingredient.Create("Mozzarella", new Money(0m), "Cheese");
    private static Ingredient Mushroom() => Ingredient.Create("Mushroom", new Money(3m), "Vegetable");
    private static Ingredient Bacon() => Ingredient.Create("Bacon", new Money(5m), "Meat");

    private static MenuItem CreatePizza(string name = "Margherita") =>
        MenuItem.Create(name, MenuCategory.Pizza, new Money(25m));

    [Fact]
    public void EnsureValidCatalogConfiguration_PizzaWithoutBaseIngredients_ThrowsPizzaWithoutIngredientException()
    {
        var pizza = CreatePizza();

        var act = pizza.EnsureValidCatalogConfiguration;

        act.Should().Throw<PizzaWithoutIngredientException>();
    }

    [Fact]
    public void EnsureValidCatalogConfiguration_PizzaWithAtLeastOneBaseIngredient_DoesNotThrow()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());

        var act = pizza.EnsureValidCatalogConfiguration;

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValidCatalogConfiguration_NonPizzaWithoutBaseIngredients_DoesNotThrow()
    {
        var drink = MenuItem.Create("Cola", MenuCategory.Drink, new Money(6m));

        var act = drink.EnsureValidCatalogConfiguration;

        act.Should().NotThrow();
    }

    [Fact]
    public void AddVariant_SecondDefaultVariant_UnsetsPreviousDefault()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m), isDefault: true);

        pizza.AddVariant(small);
        pizza.AddVariant(large);

        pizza.DefaultVariant.Should().Be(large);
        pizza.Variants.Count(v => v.IsDefault).Should().Be(1);
    }

    [Fact]
    public void EnsureValidCatalogConfiguration_MultipleVariantsWithoutDefault_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m)));
        pizza.AddVariant(MenuItemVariant.Create("Large 50cm", new Money(40m)));

        var act = pizza.EnsureValidCatalogConfiguration;

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void EnsureExtraAllowed_IngredientNotInAllowedExtras_ThrowsExtraNotAllowedException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        var bacon = Bacon();

        var act = () => pizza.EnsureExtraAllowed(bacon);

        act.Should().Throw<ExtraNotAllowedException>();
    }

    [Fact]
    public void EnsureExtraAllowed_IngredientInAllowedExtras_DoesNotThrow()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        var mushroom = Mushroom();
        pizza.AllowExtra(mushroom);

        var act = () => pizza.EnsureExtraAllowed(mushroom);

        act.Should().NotThrow();
    }

    [Fact]
    public void ResolvePrice_ItemUnavailable_ThrowsMenuItemUnavailableException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        pizza.MarkUnavailable();

        var act = () => pizza.ResolvePrice(null);

        act.Should().Throw<MenuItemUnavailableException>();
    }

    [Fact]
    public void ResolvePrice_NoVariantsAndNoneRequested_ReturnsBasePrice()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());

        var (unitPrice, variantId, variantName) = pizza.ResolvePrice(null);

        unitPrice.Should().Be(pizza.BasePrice);
        variantId.Should().BeNull();
        variantName.Should().BeNull();
    }

    [Fact]
    public void ResolvePrice_NoVariantsButOneRequested_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());

        var act = () => pizza.ResolvePrice(Guid.NewGuid());

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void ResolvePrice_VariantsExistButNoneRequested_ThrowsVariantSelectionRequiredException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.ResolvePrice(null);

        act.Should().Throw<VariantSelectionRequiredException>();
    }

    [Fact]
    public void ResolvePrice_VariantNotBelongingToItem_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.ResolvePrice(Guid.NewGuid());

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void ResolvePrice_ValidVariantRequested_ReturnsVariantPriceAndNames()
    {
        var pizza = CreatePizza();
        pizza.AddBaseIngredient(Mozzarella());
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m));
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));
        pizza.AddVariant(large);

        var (unitPrice, variantId, variantName) = pizza.ResolvePrice(large.Id);

        unitPrice.Should().Be(large.Price);
        variantId.Should().Be(large.Id);
        variantName.Should().Be(large.Name);
    }

    [Fact]
    public void UpdateDetails_SetsDescriptionAndImageUrl()
    {
        var pizza = CreatePizza();

        pizza.UpdateDetails("Classic tomato & mozzarella", "https://example.com/margherita.jpg");

        pizza.Description.Should().Be("Classic tomato & mozzarella");
        pizza.ImageUrl.Should().Be("https://example.com/margherita.jpg");
    }

    [Fact]
    public void UpdateDetails_NullValues_ClearsDescriptionAndImageUrl()
    {
        var pizza = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(25m), "desc", "img");

        pizza.UpdateDetails(null, null);

        pizza.Description.Should().BeNull();
        pizza.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void SetDefaultVariant_AnotherVariant_SwitchesDefault()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m));
        pizza.AddVariant(small);
        pizza.AddVariant(large);

        pizza.SetDefaultVariant(large.Id);

        pizza.DefaultVariant.Should().Be(large);
        small.IsDefault.Should().BeFalse();
        pizza.Variants.Count(v => v.IsDefault).Should().Be(1);
    }

    [Fact]
    public void SetDefaultVariant_AlreadyDefault_IsIdempotent()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        pizza.AddVariant(small);

        var act = () => pizza.SetDefaultVariant(small.Id);

        act.Should().NotThrow();
        pizza.DefaultVariant.Should().Be(small);
    }

    [Fact]
    public void SetDefaultVariant_UnknownVariantId_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.SetDefaultVariant(Guid.NewGuid());

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void RemoveVariant_NonDefaultVariantWithOthersRemaining_RemovesIt()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m));
        pizza.AddVariant(small);
        pizza.AddVariant(large);

        pizza.RemoveVariant(large.Id);

        pizza.Variants.Should().ContainSingle().Which.Should().Be(small);
    }

    [Fact]
    public void RemoveVariant_UnknownVariantId_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.RemoveVariant(Guid.NewGuid());

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void RemoveVariant_OnlyRemainingVariant_ThrowsCannotRemoveLastVariantException()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        pizza.AddVariant(small);

        var act = () => pizza.RemoveVariant(small.Id);

        act.Should().Throw<CannotRemoveLastVariantException>();
    }

    [Fact]
    public void RemoveVariant_DefaultVariantWithOthersRemaining_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m));
        pizza.AddVariant(small);
        pizza.AddVariant(large);

        var act = () => pizza.RemoveVariant(small.Id);

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void RemoveVariant_DefaultVariantAfterReassigningDefault_Succeeds()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        var large = MenuItemVariant.Create("Large 50cm", new Money(40m));
        pizza.AddVariant(small);
        pizza.AddVariant(large);

        pizza.SetDefaultVariant(large.Id);
        pizza.RemoveVariant(small.Id);

        pizza.Variants.Should().ContainSingle().Which.Should().Be(large);
        pizza.DefaultVariant.Should().Be(large);
    }

    [Fact]
    public void RenameVariant_ExistingVariant_ChangesName()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        pizza.AddVariant(small);

        pizza.RenameVariant(small.Id, "Piccola 30cm");

        small.Name.Should().Be("Piccola 30cm");
    }

    [Fact]
    public void RenameVariant_UnknownVariantId_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.RenameVariant(Guid.NewGuid(), "New name");

        act.Should().Throw<InvalidVariantConfigurationException>();
    }

    [Fact]
    public void RenameVariant_EmptyName_ThrowsArgumentException()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        pizza.AddVariant(small);

        var act = () => pizza.RenameVariant(small.Id, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateVariantPrice_ExistingVariant_ChangesPrice()
    {
        var pizza = CreatePizza();
        var small = MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true);
        pizza.AddVariant(small);

        pizza.UpdateVariantPrice(small.Id, new Money(28m));

        small.Price.Amount.Should().Be(28m);
    }

    [Fact]
    public void UpdateVariantPrice_UnknownVariantId_ThrowsInvalidVariantConfigurationException()
    {
        var pizza = CreatePizza();
        pizza.AddVariant(MenuItemVariant.Create("Small 30cm", new Money(25m), isDefault: true));

        var act = () => pizza.UpdateVariantPrice(Guid.NewGuid(), new Money(10m));

        act.Should().Throw<InvalidVariantConfigurationException>();
    }
}
