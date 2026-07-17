using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Catalog.Validators;
using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class UpdateMenuItemCommandValidatorTests
{
    private readonly UpdateMenuItemCommandValidator _validator = new();

    private static UpdateMenuItemCommand ValidCommand() => new(
        Guid.NewGuid(),
        "Margherita",
        "Classic",
        null,
        new MoneyDto(25m, "PLN"),
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
    public void Validate_EmptyId_HasErrorForId()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMenuItemCommand.Id));
    }

    [Fact]
    public void Validate_NegativeVariantPrice_HasErrorForVariantPriceAmount()
    {
        var command = ValidCommand() with
        {
            Variants = new[] { new MenuItemVariantInputDto(Guid.NewGuid(), "Small", new MoneyDto(-5m, "PLN"), true) },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Price.Amount"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void Validate_ZeroOrOneDefaultVariant_HasNoErrors(bool firstIsDefault, bool secondIsDefault)
    {
        var command = ValidCommand() with
        {
            Variants = new[]
            {
                new MenuItemVariantInputDto(Guid.NewGuid(), "Small", new MoneyDto(20m, "PLN"), firstIsDefault),
                new MenuItemVariantInputDto(Guid.NewGuid(), "Large", new MoneyDto(30m, "PLN"), secondIsDefault),
            },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MoreThanOneDefaultVariant_HasErrorForVariants()
    {
        var command = ValidCommand() with
        {
            Variants = new[]
            {
                new MenuItemVariantInputDto(Guid.NewGuid(), "Small", new MoneyDto(20m, "PLN"), true),
                new MenuItemVariantInputDto(Guid.NewGuid(), "Large", new MoneyDto(30m, "PLN"), true),
            },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMenuItemCommand.Variants));
    }
}
