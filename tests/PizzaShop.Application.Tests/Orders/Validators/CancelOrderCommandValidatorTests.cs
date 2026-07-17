using FluentAssertions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_HasNoErrors()
    {
        var result = _validator.Validate(new CancelOrderCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasErrorForOrderId()
    {
        var result = _validator.Validate(new CancelOrderCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CancelOrderCommand.OrderId));
    }
}
