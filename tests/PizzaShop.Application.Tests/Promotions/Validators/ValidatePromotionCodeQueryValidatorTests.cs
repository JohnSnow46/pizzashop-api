using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Promotions.Queries;
using PizzaShop.Application.Promotions.Validators;

namespace PizzaShop.Application.Tests.Promotions.Validators;

public class ValidatePromotionCodeQueryValidatorTests
{
    private readonly ValidatePromotionCodeQueryValidator _validator = new();

    private static ValidatePromotionCodeQuery ValidQuery() =>
        new("SUMMER10", new MoneyDto(100m, "PLN"), new MoneyDto(10m, "PLN"));

    [Fact]
    public void Validate_ValidQuery_HasNoErrors()
    {
        var result = _validator.Validate(ValidQuery());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCode_HasErrorForCode()
    {
        var query = ValidQuery() with { Code = "" };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ValidatePromotionCodeQuery.Code));
    }

    [Fact]
    public void Validate_NegativeSubtotal_HasErrorForSubtotalAmount()
    {
        var query = ValidQuery() with { Subtotal = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subtotal.Amount");
    }

    [Fact]
    public void Validate_NegativeDeliveryFee_HasErrorForDeliveryFeeAmount()
    {
        var query = ValidQuery() with { DeliveryFee = new MoneyDto(-1m, "PLN") };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DeliveryFee.Amount");
    }
}
