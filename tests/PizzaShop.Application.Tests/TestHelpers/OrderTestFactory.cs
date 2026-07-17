using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Application.Tests.TestHelpers;

/// <summary>Shared builders for Order-related Application tests.</summary>
internal static class OrderTestFactory
{
    public static readonly GeoCoordinate RestaurantLocation = new(52.2297, 21.0122); // Warsaw center
    public static readonly GeoCoordinate NearbyPoint = new(52.23, 21.02); // a few hundred meters away

    public static DomainRestaurant CreateOpenRestaurant()
    {
        var allDayEveryDay = new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            allDayEveryDay[day] = new List<TimeRange> { new(new TimeOnly(0, 0), new TimeOnly(23, 59)) };

        return DomainRestaurant.Create(
            "Pizza Palace",
            new Address("Main St", "1", "Warsaw", "00-001"),
            RestaurantLocation,
            deliveryRadiusKm: 5,
            timeZoneId: "UTC",
            openingHours: new OpeningHours(allDayEveryDay),
            contactPhone: "123456789",
            deliveryFee: new Money(10));
    }

    public static ContactDetails SampleContact() => new("Jan Kowalski", "123456789");

    public static DeliveryAddress SampleDeliveryAddress() =>
        new(new Address("Client St", "2", "Warsaw", "00-002"), NearbyPoint);

    /// <summary>Builds a ready-to-persist pizza <c>MenuItem</c> with one base ingredient.</summary>
    public static MenuItem CreatePizza(decimal price = 30m)
    {
        var pizza = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(price));
        pizza.AddBaseIngredient(Ingredient.Create("Cheese", Money.Zero()));
        return pizza;
    }

    public static Order CreateOrder(
        DomainRestaurant? restaurant = null,
        Guid? customerId = null,
        PaymentMethod paymentMethod = PaymentMethod.OnPickup,
        FulfillmentType fulfillmentType = FulfillmentType.Pickup)
    {
        restaurant ??= CreateOpenRestaurant();
        var pizza = CreatePizza();
        var item = OrderItem.Create(pizza.Id, pizza.Name, pizza.BasePrice, 1);

        var deliveryAddress = fulfillmentType == FulfillmentType.Delivery ? SampleDeliveryAddress() : null;

        return Order.Create(
            "ORD-0001",
            customerId,
            SampleContact(),
            fulfillmentType,
            deliveryAddress,
            new[] { item },
            DateTimeOffset.UtcNow,
            null,
            paymentMethod,
            restaurant);
    }
}
