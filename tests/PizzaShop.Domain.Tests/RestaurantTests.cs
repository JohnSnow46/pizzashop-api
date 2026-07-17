using FluentAssertions;
using PizzaShop.Domain;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests;

public class RestaurantTests
{
    private static readonly GeoCoordinate WarsawCenter = new(52.2297, 21.0122);

    private static Restaurant CreateRestaurant(double deliveryRadiusKm = 5, bool acceptOrders = true)
    {
        var restaurant = Restaurant.Create(
            "PizzaShop",
            new Address("Main St", "1", "Warsaw", "00-001"),
            WarsawCenter,
            deliveryRadiusKm,
            "Europe/Warsaw",
            new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
            {
                [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(10, 0), new TimeOnly(22, 0)) },
            }),
            "123456789",
            new Money(10m));

        if (!acceptOrders)
            restaurant.StopAcceptingOrders();

        return restaurant;
    }

    [Fact]
    public void Create_DeliveryRadiusZeroOrLess_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreateRestaurant(deliveryRadiusKm: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsWithinDeliveryArea_PointWithinRadius_ReturnsTrue()
    {
        var restaurant = CreateRestaurant(deliveryRadiusKm: 5);
        var nearbyPoint = new GeoCoordinate(52.23, 21.02); // a few hundred meters away

        restaurant.IsWithinDeliveryArea(nearbyPoint).Should().BeTrue();
    }

    [Fact]
    public void IsWithinDeliveryArea_PointOutsideRadius_ReturnsFalse()
    {
        var restaurant = CreateRestaurant(deliveryRadiusKm: 1);
        var krakow = new GeoCoordinate(50.0647, 19.9450);

        restaurant.IsWithinDeliveryArea(krakow).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptOrderAt_OpenAndAccepting_ReturnsTrue()
    {
        var restaurant = CreateRestaurant();
        var mondayNoonUtc = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero); // 12:00 Warsaw time

        restaurant.CanAcceptOrderAt(mondayNoonUtc).Should().BeTrue();
    }

    [Fact]
    public void CanAcceptOrderAt_NotAcceptingOrders_ReturnsFalse()
    {
        var restaurant = CreateRestaurant(acceptOrders: false);
        var mondayNoonUtc = new DateTimeOffset(2024, 1, 1, 11, 0, 0, TimeSpan.Zero);

        restaurant.CanAcceptOrderAt(mondayNoonUtc).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptOrderAt_OutsideOpeningHours_ReturnsFalse()
    {
        var restaurant = CreateRestaurant();
        var mondayMidnightUtc = new DateTimeOffset(2024, 1, 1, 23, 30, 0, TimeSpan.Zero); // 00:30 Warsaw time, closed

        restaurant.CanAcceptOrderAt(mondayMidnightUtc).Should().BeFalse();
    }

    [Fact]
    public void UpdateDeliveryArea_RadiusZeroOrLess_ThrowsArgumentOutOfRangeException()
    {
        var restaurant = CreateRestaurant();

        var act = () => restaurant.UpdateDeliveryArea(WarsawCenter, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
