using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Application.Orders.Validators;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand ValidPickupCommand() => new(
        new ContactDetailsDto("Jan Kowalski", "123456789"),
        FulfillmentType.Pickup,
        null,
        new[] { new CreateOrderItemDto(Guid.NewGuid(), null, 1, Array.Empty<Guid>()) },
        null,
        PaymentMethod.OnPickup);

    [Fact]
    public void Validate_ValidPickupCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidPickupCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidDeliveryCommand_HasNoErrors()
    {
        var command = ValidPickupCommand() with
        {
            FulfillmentType = FulfillmentType.Delivery,
            DeliveryAddress = new AddressDto("Client St", "2", "Warsaw", "00-002"),
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullContact_HasErrorForContact()
    {
        var command = ValidPickupCommand() with { Contact = null! };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.Contact));
    }

    [Fact]
    public void Validate_EmptyItems_HasErrorForItems()
    {
        var command = ValidPickupCommand() with { Items = Array.Empty<CreateOrderItemDto>() };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.Items));
    }

    [Fact]
    public void Validate_ItemQuantityBelowOne_HasErrorForQuantity()
    {
        var command = ValidPickupCommand() with
        {
            Items = new[] { new CreateOrderItemDto(Guid.NewGuid(), null, 0, Array.Empty<Guid>()) },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Quantity"));
    }

    [Fact]
    public void Validate_DeliveryWithoutAddress_HasErrorForDeliveryAddress()
    {
        var command = ValidPickupCommand() with { FulfillmentType = FulfillmentType.Delivery, DeliveryAddress = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.DeliveryAddress));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("+48123456789")]
    [InlineData("+48 123 456 789")]
    [InlineData("123-456-789")]
    [InlineData("+48-123-456-789")]
    public void Validate_ValidPhoneNumberFormats_HasNoErrors(string phoneNumber)
    {
        var command = ValidPickupCommand() with
        {
            Contact = ValidPickupCommand().Contact with { PhoneNumber = phoneNumber },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdefghi")]
    [InlineData("123456789012345")]
    [InlineData("48-123-456-789")]
    public void Validate_InvalidPhoneNumberFormats_HasErrorForPhoneNumber(string phoneNumber)
    {
        var command = ValidPickupCommand() with
        {
            Contact = ValidPickupCommand().Contact with { PhoneNumber = phoneNumber },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Contact.PhoneNumber");
    }

    [Fact]
    public void Validate_NoEmail_HasNoErrors()
    {
        var command = ValidPickupCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidEmail_HasNoErrors()
    {
        var command = ValidPickupCommand() with
        {
            Contact = ValidPickupCommand().Contact with { Email = "jan.kowalski@example.com" },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEmail_HasErrorForEmail()
    {
        var command = ValidPickupCommand() with
        {
            Contact = ValidPickupCommand().Contact with { Email = "not-an-email" },
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Contact.Email");
    }

    [Fact]
    public void Validate_NoPointsToRedeem_HasNoErrors()
    {
        var command = ValidPickupCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativePointsToRedeem_HasErrorForPointsToRedeem()
    {
        var command = ValidPickupCommand() with { PointsToRedeem = -1 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrderCommand.PointsToRedeem));
    }

    [Fact]
    public void Validate_ZeroPointsToRedeem_HasNoErrors()
    {
        var command = ValidPickupCommand() with { PointsToRedeem = 0 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
