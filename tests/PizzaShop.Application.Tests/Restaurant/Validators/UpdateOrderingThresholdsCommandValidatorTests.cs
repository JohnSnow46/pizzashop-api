using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Validators;

namespace PizzaShop.Application.Tests.Restaurant.Validators;

public class UpdateOrderingThresholdsCommandValidatorTests
{
    private readonly UpdateOrderingThresholdsCommandValidator _validator = new();

    private static UpdateOrderingThresholdsCommand ValidCommand() => new(
        new MoneyDto(20m, "PLN"),
        new MoneyDto(50m, "PLN"),
        new MoneyDto(5m, "PLN"));

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidCommandWithNullOptionalThresholds_HasNoErrors()
    {
        var command = ValidCommand() with { MinimumOrderValue = null, FreeDeliveryThreshold = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeDeliveryFee_HasErrorForDeliveryFeeAmount()
    {
        var command = ValidCommand() with { DeliveryFee = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DeliveryFee.Amount");
    }

    [Fact]
    public void Validate_NegativeMinimumOrderValue_HasErrorForMinimumOrderValueAmount()
    {
        var command = ValidCommand() with { MinimumOrderValue = new MoneyDto(-10m, "PLN") };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinimumOrderValue.Amount");
    }
}
