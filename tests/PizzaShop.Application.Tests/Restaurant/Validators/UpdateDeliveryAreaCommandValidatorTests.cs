using FluentAssertions;
using PizzaShop.Application.Restaurant.Commands;
using PizzaShop.Application.Restaurant.Validators;

namespace PizzaShop.Application.Tests.Restaurant.Validators;

public class UpdateDeliveryAreaCommandValidatorTests
{
    private readonly UpdateDeliveryAreaCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(new UpdateDeliveryAreaCommand(52.2297, 21.0122, 5));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DeliveryRadiusNotPositive_HasErrorForDeliveryRadiusKm()
    {
        var result = _validator.Validate(new UpdateDeliveryAreaCommand(52.2297, 21.0122, 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateDeliveryAreaCommand.DeliveryRadiusKm));
    }

    [Fact]
    public void Validate_LatitudeOutOfRange_HasErrorForLatitude()
    {
        var result = _validator.Validate(new UpdateDeliveryAreaCommand(91, 21.0122, 5));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateDeliveryAreaCommand.Latitude));
    }
}
