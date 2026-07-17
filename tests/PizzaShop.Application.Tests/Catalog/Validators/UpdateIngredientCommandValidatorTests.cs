using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Validators;
using PizzaShop.Application.Common.Dtos;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class UpdateIngredientCommandValidatorTests
{
    private readonly UpdateIngredientCommandValidator _validator = new();

    private static UpdateIngredientCommand ValidCommand() => new(Guid.NewGuid(), "Mozzarella", new MoneyDto(2m, "PLN"), true);

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateIngredientCommand.Id));
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
