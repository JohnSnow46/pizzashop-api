using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Validators;
using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class CreateIngredientCommandValidatorTests
{
    private readonly CreateIngredientCommandValidator _validator = new();

    private static CreateIngredientCommand ValidCommand() => new("Mozzarella", new MoneyDto(2m, "PLN"), "Cheese");

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateIngredientCommand.Name));
    }

    [Fact]
    public void Validate_NegativeExtraPrice_HasErrorForExtraPriceAmount()
    {
        var command = ValidCommand() with { ExtraPrice = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExtraPrice.Amount");
    }
}
