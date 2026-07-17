using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Application.Tests.TestHelpers;

internal static class RestaurantTestFactory
{
    public static DomainRestaurant Create()
    {
        var openingHours = new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(10, 0), new TimeOnly(22, 0)) },
        });

        return DomainRestaurant.Create(
            "Pizza Palace",
            new Address("Main St", "1", "Warsaw", "00-001"),
            new GeoCoordinate(52.2297, 21.0122),
            deliveryRadiusKm: 5,
            timeZoneId: "Europe/Warsaw",
            openingHours: openingHours,
            contactPhone: "123456789",
            deliveryFee: new Money(10));
    }
}
