using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Catalog.Validators;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class CreateMenuItemCommandValidatorTests
{
    private readonly CreateMenuItemCommandValidator _validator = new();

    private static CreateMenuItemCommand ValidCommand() => new(
        "Margherita",
        MenuCategory.Pizza,
        new MoneyDto(25m, "PLN"),
        "Classic",
        null,
        Array.Empty<Guid>(),
        Array.Empty<Guid>(),
        Array.Empty<MenuItemVariantInputDto>());

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_HasErrorForName()
    {
        var command = ValidCommand() with { Name = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateMenuItemCommand.Name));
    }

    [Fact]
    public void Validate_NegativeBasePrice_HasErrorForBasePriceAmount()
    {
        var command = ValidCommand() with { BasePrice = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BasePrice.Amount");
    }

    [Fact]
    public void Validate_VariantWithEmptyName_HasErrorForVariantName()
    {
        var command = ValidCommand() with
        {
            Variants = new[] { new MenuItemVariantInputDto(null, "", new MoneyDto(10m, "PLN"), true) },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Name"));
    }
}
