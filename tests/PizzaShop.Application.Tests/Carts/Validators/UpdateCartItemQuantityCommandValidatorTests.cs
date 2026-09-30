using FluentAssertions;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Carts.Validators;

namespace PizzaShop.Application.Tests.Carts.Validators;

public class UpdateCartItemQuantityCommandValidatorTests
{
    private readonly UpdateCartItemQuantityCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateCartItemQuantityCommand(Guid.NewGuid(), 5));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCartItemId_HasErrorForCartItemId()
    {
        var result = _validator.Validate(new UpdateCartItemQuantityCommand(Guid.Empty, 5));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCartItemQuantityCommand.CartItemId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_QuantityOutOfRange_HasErrorForQuantity(int quantity)
    {
        var result = _validator.Validate(new UpdateCartItemQuantityCommand(Guid.NewGuid(), quantity));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCartItemQuantityCommand.Quantity));
    }
}
