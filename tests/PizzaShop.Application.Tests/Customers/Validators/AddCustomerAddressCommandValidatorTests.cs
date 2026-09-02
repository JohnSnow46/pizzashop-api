using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Customers.Commands;
using PizzaShop.Application.Customers.Validators;

namespace PizzaShop.Application.Tests.Customers.Validators;

public class AddCustomerAddressCommandValidatorTests
{
    private readonly AddCustomerAddressCommandValidator _validator = new();

    private static AddCustomerAddressCommand ValidCommand() =>
        new("Home", new AddressDto("Main St", "12", "Warsaw", "00-001"), true);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyLabel_HasErrorForLabel()
    {
        var command = ValidCommand() with { Label = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCustomerAddressCommand.Label));
    }

    [Fact]
    public void Validate_LabelTooLong_HasErrorForLabel()
    {
        var command = ValidCommand() with { Label = new string('a', 101) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddCustomerAddressCommand.Label));
    }

    [Fact]
    public void Validate_StreetTooLong_HasErrorForAddressStreet()
    {
        var command = ValidCommand() with { Address = ValidCommand().Address with { Street = new string('a', 201) } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address.Street");
    }

    [Fact]
    public void Validate_PostalCodeTooLong_HasErrorForAddressPostalCode()
    {
        var command = ValidCommand() with { Address = ValidCommand().Address with { PostalCode = new string('1', 11) } };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address.PostalCode");
    }
}
