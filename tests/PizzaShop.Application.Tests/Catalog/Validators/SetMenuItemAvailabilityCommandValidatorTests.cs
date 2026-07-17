using FluentAssertions;
using PizzaShop.Application.Catalog.Commands;
using PizzaShop.Application.Catalog.Validators;

namespace PizzaShop.Application.Tests.Catalog.Validators;

public class SetMenuItemAvailabilityCommandValidatorTests
{
    private readonly SetMenuItemAvailabilityCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new SetMenuItemAvailabilityCommand(Guid.NewGuid(), true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMenuItemId_HasErrorForMenuItemId()
    {
        var result = _validator.Validate(new SetMenuItemAvailabilityCommand(Guid.Empty, true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetMenuItemAvailabilityCommand.MenuItemId));
    }
}
