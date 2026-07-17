using FluentAssertions;
using PizzaShop.Application.Payments.Commands;
using PizzaShop.Application.Payments.Validators;

namespace PizzaShop.Application.Tests.Payments.Validators;

public class ConfirmPaymentFromNotificationCommandValidatorTests
{
    private readonly ConfirmPaymentFromNotificationCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyRawBodyAndHeaders_HasNoErrors()
    {
        var result = _validator.Validate(new ConfirmPaymentFromNotificationCommand("{}", new Dictionary<string, string>()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyRawBody_HasErrorForRawBody()
    {
        var result = _validator.Validate(new ConfirmPaymentFromNotificationCommand(string.Empty, new Dictionary<string, string>()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmPaymentFromNotificationCommand.RawBody));
    }

    [Fact]
    public void Validate_NullHeaders_HasErrorForHeaders()
    {
        var result = _validator.Validate(new ConfirmPaymentFromNotificationCommand("{}", null!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConfirmPaymentFromNotificationCommand.Headers));
    }
}
