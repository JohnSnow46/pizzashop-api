using FluentAssertions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class StartPreparationCommandValidatorTests
{
    private readonly StartPreparationCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyOrderId_HasNoErrors()
    {
        var result = _validator.Validate(new StartPreparationCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasErrorForOrderId()
    {
        var result = _validator.Validate(new StartPreparationCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPreparationCommand.OrderId));
    }
}
