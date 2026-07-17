using FluentAssertions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class RejectOrderCommandValidatorTests
{
    private readonly RejectOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_HasNoErrors()
    {
        var result = _validator.Validate(new RejectOrderCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasErrorForOrderId()
    {
        var result = _validator.Validate(new RejectOrderCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectOrderCommand.OrderId));
    }
}
