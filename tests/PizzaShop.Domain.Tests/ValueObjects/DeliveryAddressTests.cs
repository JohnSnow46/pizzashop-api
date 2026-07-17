using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class DeliveryAddressTests
{
    private static Address SampleAddress() => new("Main St", "1", "Warsaw", "00-001");
    private static GeoCoordinate SampleCoordinate() => new(52.0, 21.0);

    [Fact]
    public void Constructor_NullAddress_ThrowsArgumentNullException()
    {
        var act = () => new DeliveryAddress(null!, SampleCoordinate());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullCoordinate_ThrowsArgumentNullException()
    {
        var act = () => new DeliveryAddress(SampleAddress(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equals_SameAddressAndCoordinate_ReturnsTrue()
    {
        var a = new DeliveryAddress(SampleAddress(), SampleCoordinate());
        var b = new DeliveryAddress(SampleAddress(), SampleCoordinate());

        (a == b).Should().BeTrue();
    }
}
