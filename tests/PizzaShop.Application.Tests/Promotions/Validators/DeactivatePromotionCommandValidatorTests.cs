using FluentAssertions;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Application.Promotions.Validators;

namespace PizzaShop.Application.Tests.Promotions.Validators;

public class DeactivatePromotionCommandValidatorTests
{
    private readonly DeactivatePromotionCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyPromotionId_HasNoErrors()
    {
        var result = _validator.Validate(new DeactivatePromotionCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPromotionId_HasErrorForPromotionId()
    {
        var result = _validator.Validate(new DeactivatePromotionCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeactivatePromotionCommand.PromotionId));
    }
}
