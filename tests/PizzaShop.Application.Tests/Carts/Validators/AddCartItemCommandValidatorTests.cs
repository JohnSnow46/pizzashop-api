using FluentAssertions;
using PizzaShop.Application.Carts.Commands;
using PizzaShop.Application.Carts.Validators;

namespace PizzaShop.Application.Tests.Carts.Validators;

public class AddCartItemCommandValidatorTests
{
    private readonly AddCartItemCommandValidator _validator = new();

    private static AddCartItemCommand ValidCommand() => new(Guid.NewGuid(), null, [], 1, null);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMenuItemId_HasErrorForMenuItemId()
    {
        var result = _validator.Validate(ValidCommand() with { MenuItemId = Guid.Empty });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCartItemCommand.MenuItemId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_QuantityOutOfRange_HasErrorForQuantity(int quantity)
    {
        var result = _validator.Validate(ValidCommand() with { Quantity = quantity });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCartItemCommand.Quantity));
    }

    [Fact]
    public void Validate_NotesExceedingMaxLength_HasErrorForNotes()
    {
        var result = _validator.Validate(ValidCommand() with { Notes = new string('a', 501) });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCartItemCommand.Notes));
    }
}
