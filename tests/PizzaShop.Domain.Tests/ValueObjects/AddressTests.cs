using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Constructor_AllRequiredFieldsProvided_DoesNotThrow()
    {
        var act = () => new Address("Main St", "1", "Warsaw", "00-001");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "1", "Warsaw", "00-001")]
    [InlineData("Main St", "", "Warsaw", "00-001")]
    [InlineData("Main St", "1", "", "00-001")]
    [InlineData("Main St", "1", "Warsaw", "")]
    public void Constructor_MissingRequiredField_ThrowsArgumentException(string street, string buildingNumber, string city, string postalCode)
    {
        var act = () => new Address(street, buildingNumber, city, postalCode);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new Address("Main St", "1", "Warsaw", "00-001", "5");
        var b = new Address("Main St", "1", "Warsaw", "00-001", "5");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentApartmentNumber_ReturnsFalse()
    {
        var a = new Address("Main St", "1", "Warsaw", "00-001", "5");
        var b = new Address("Main St", "1", "Warsaw", "00-001", "6");

        (a == b).Should().BeFalse();
    }
}
