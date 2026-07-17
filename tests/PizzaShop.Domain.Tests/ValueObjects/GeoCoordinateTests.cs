using FluentAssertions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.ValueObjects;

public class GeoCoordinateTests
{
    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    public void Constructor_LatitudeOutOfRange_ThrowsArgumentOutOfRangeException(double lat, double lon)
    {
        var act = () => new GeoCoordinate(lat, lon);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Constructor_LongitudeOutOfRange_ThrowsArgumentOutOfRangeException(double lat, double lon)
    {
        var act = () => new GeoCoordinate(lat, lon);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_BoundaryValues_DoesNotThrow()
    {
        var act = () => new GeoCoordinate(90, 180);

        act.Should().NotThrow();
    }

    [Fact]
    public void DistanceKmTo_SamePoint_ReturnsZero()
    {
        var point = new GeoCoordinate(52.2297, 21.0122);

        point.DistanceKmTo(point).Should().Be(0);
    }

    [Fact]
    public void DistanceKmTo_WarsawToKrakow_ReturnsApproximateKnownDistance()
    {
        var warsaw = new GeoCoordinate(52.2297, 21.0122);
        var krakow = new GeoCoordinate(50.0647, 19.9450);

        var distance = warsaw.DistanceKmTo(krakow);

        distance.Should().BeApproximately(252, 5);
    }

    [Fact]
    public void DistanceKmTo_IsSymmetric()
    {
        var a = new GeoCoordinate(52.2297, 21.0122);
        var b = new GeoCoordinate(50.0647, 19.9450);

        a.DistanceKmTo(b).Should().BeApproximately(b.DistanceKmTo(a), 0.0001);
    }

    [Fact]
    public void Equals_SameCoordinates_ReturnsTrue()
    {
        var a = new GeoCoordinate(52.0, 21.0);
        var b = new GeoCoordinate(52.0, 21.0);

        (a == b).Should().BeTrue();
    }
}
