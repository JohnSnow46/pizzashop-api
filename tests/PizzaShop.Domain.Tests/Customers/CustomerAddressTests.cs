using FluentAssertions;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Customers;

public class CustomerAddressTests
{
    private static DeliveryAddress SampleAddress() =>
        new(new Address("Main St", "1", "Warsaw", "00-001"), new GeoCoordinate(52.0, 21.0));

    [Fact]
    public void Update_MissingLabel_ThrowsArgumentException()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var entry = customer.AddAddress("Home", SampleAddress());

        var act = () => entry.Update("", SampleAddress());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ValidValues_UpdatesLabelAndAddress()
    {
        var customer = Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var entry = customer.AddAddress("Home", SampleAddress());
        var newAddress = new DeliveryAddress(new Address("Other St", "5", "Warsaw", "00-002"), new GeoCoordinate(52.1, 21.1));

        entry.Update("Cottage", newAddress);

        entry.Label.Should().Be("Cottage");
        entry.DeliveryAddress.Should().Be(newAddress);
    }
}
