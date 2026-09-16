using FluentAssertions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Orders.Queries;
using PizzaShop.Application.Orders.Validators;

namespace PizzaShop.Application.Tests.Orders.Validators;

public class CheckDeliveryAvailabilityQueryValidatorTests
{
    private readonly CheckDeliveryAvailabilityQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAddress_HasNoErrors()
    {
        var query = new CheckDeliveryAvailabilityQuery(new AddressDto("Client St", "2", "Warsaw", "00-002"));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullAddress_HasErrorForAddress()
    {
        var query = new CheckDeliveryAvailabilityQuery(null!);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckDeliveryAvailabilityQuery.Address));
    }

    [Fact]
    public void Validate_EmptyStreet_HasErrorForStreet()
    {
        var query = new CheckDeliveryAvailabilityQuery(new AddressDto("", "2", "Warsaw", "00-002"));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Street"));
    }

    [Fact]
    public void Validate_StreetOverMaxLength_HasErrorForStreet()
    {
        var query = new CheckDeliveryAvailabilityQuery(new AddressDto(new string('a', 201), "2", "Warsaw", "00-002"));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Street"));
    }

    [Fact]
    public void Validate_PostalCodeOverMaxLength_HasErrorForPostalCode()
    {
        var query = new CheckDeliveryAvailabilityQuery(new AddressDto("Client St", "2", "Warsaw", new string('1', 11)));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("PostalCode"));
    }
}
