using FluentAssertions;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Payments.Validators;

namespace PizzaShop.Application.Tests.Payments.Validators;

public class InitializeGuestPaymentCommandValidatorTests
{
    private readonly InitializeGuestPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyToken_HasNoErrors()
    {
        var result = _validator.Validate(new InitializeGuestPaymentCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyToken_HasErrorForGuestTrackingToken()
    {
        var result = _validator.Validate(new InitializeGuestPaymentCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InitializeGuestPaymentCommand.GuestTrackingToken));
    }
}
