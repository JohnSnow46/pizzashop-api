using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Application.Promotions.Validators;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Promotions.Validators;

public class CreatePromotionCommandValidatorTests
{
    private readonly CreatePromotionCommandValidator _validator = new();

    private static CreatePromotionCommand ValidCommand() => new(
        "10% off",
        PromotionType.Percentage,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddDays(30),
        10m,
        "SUMMER10",
        null,
        null);

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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.Name));
    }

    [Fact]
    public void Validate_BuyXGetYWithValidRule_HasNoErrors()
    {
        var command = ValidCommand() with
        {
            Type = PromotionType.BuyXGetY,
            Value = null,
            BuyXGetY = new BuyXGetYRuleDto(Guid.NewGuid(), 2, Guid.NewGuid(), 1, 100m),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BuyXGetYWithoutRule_HasErrorForBuyXGetY()
    {
        var command = ValidCommand() with { Type = PromotionType.BuyXGetY, Value = null, BuyXGetY = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.BuyXGetY));
    }

    [Fact]
    public void Validate_BuyXGetYWithValue_HasErrorForValue()
    {
        var command = ValidCommand() with
        {
            Type = PromotionType.BuyXGetY,
            Value = 10m,
            BuyXGetY = new BuyXGetYRuleDto(Guid.NewGuid(), 2, Guid.NewGuid(), 1, 100m),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_BuyXGetYWithInvalidBuyQuantity_HasErrorForBuyQuantity(int buyQuantity)
    {
        var command = ValidCommand() with
        {
            Type = PromotionType.BuyXGetY,
            Value = null,
            BuyXGetY = new BuyXGetYRuleDto(Guid.NewGuid(), buyQuantity, Guid.NewGuid(), 1, 100m),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyXGetY.BuyQuantity");
    }

    [Fact]
    public void Validate_NonBuyXGetYTypeWithBuyXGetYConfigured_HasErrorForBuyXGetY()
    {
        var command = ValidCommand() with
        {
            Type = PromotionType.Percentage,
            BuyXGetY = new BuyXGetYRuleDto(Guid.NewGuid(), 2, Guid.NewGuid(), 1, 100m),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.BuyXGetY));
    }

    [Fact]
    public void Validate_ValidToBeforeValidFrom_HasErrorForValidTo()
    {
        var command = ValidCommand() with { ValidFrom = DateTimeOffset.UtcNow, ValidTo = DateTimeOffset.UtcNow.AddDays(-1) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.ValidTo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PercentageValueOutOfRange_HasErrorForValue(decimal value)
    {
        var command = ValidCommand() with { Value = value };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.Value));
    }

    [Fact]
    public void Validate_FixedAmountWithoutValue_HasErrorForValue()
    {
        var command = ValidCommand() with { Type = PromotionType.FixedAmount, Value = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.Value));
    }

    [Fact]
    public void Validate_FreeDeliveryWithoutValue_HasNoErrors()
    {
        var command = ValidCommand() with { Type = PromotionType.FreeDelivery, Value = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UsageLimitZero_HasErrorForUsageLimit()
    {
        var command = ValidCommand() with { UsageLimit = 0 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePromotionCommand.UsageLimit));
    }

    [Fact]
    public void Validate_NegativeMinOrderValue_HasErrorForMinOrderValueAmount()
    {
        var command = ValidCommand() with { MinOrderValue = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinOrderValue.Amount");
    }
}
