using PizzaShop.Domain;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Orders;

/// <summary>Shared restaurant/address builders reused across <c>Order</c> aggregate tests.</summary>
internal static class OrderTestFixtures
{
    public static readonly GeoCoordinate RestaurantLocation = new(52.2297, 21.0122); // Warsaw center
    public static readonly GeoCoordinate NearbyPoint = new(52.23, 21.02); // a few hundred meters away
    public static readonly GeoCoordinate FarAwayPoint = new(50.0647, 19.9450); // Krakow, ~250km away

    public static Restaurant CreateOpenAllWeekRestaurant(
        Money? minimumOrderValue = null,
        Money? freeDeliveryThreshold = null,
        Money? deliveryFee = null,
        double deliveryRadiusKm = 5)
    {
        var allDayEveryDay = new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            allDayEveryDay[day] = new List<TimeRange> { new(new TimeOnly(0, 0), new TimeOnly(23, 59)) };

        return Restaurant.Create(
            "PizzaShop",
            new Address("Main St", "1", "Warsaw", "00-001"),
            RestaurantLocation,
            deliveryRadiusKm,
            "UTC",
            new OpeningHours(allDayEveryDay),
            "123456789",
            deliveryFee ?? new Money(10m),
            minimumOrderValue,
            freeDeliveryThreshold);
    }

    public static Restaurant CreateClosedRestaurant()
    {
        var restaurant = CreateOpenAllWeekRestaurant();
        restaurant.StopAcceptingOrders();
        return restaurant;
    }

    public static ContactDetails SampleContact() => new("Jan Kowalski", "123456789");

    public static DeliveryAddress SampleDeliveryAddress(GeoCoordinate? coordinate = null) =>
        new(new Address("Client St", "2", "Warsaw", "00-002"), coordinate ?? NearbyPoint);
}
