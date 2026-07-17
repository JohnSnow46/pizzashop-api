using FluentAssertions;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Payments.Validators;

namespace PizzaShop.Application.Tests.Payments.Validators;

public class InitializePaymentCommandValidatorTests
{
    private readonly InitializePaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_HasNoErrors()
    {
        var result = _validator.Validate(new InitializePaymentCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasErrorForOrderId()
    {
        var result = _validator.Validate(new InitializePaymentCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InitializePaymentCommand.OrderId));
    }
}
