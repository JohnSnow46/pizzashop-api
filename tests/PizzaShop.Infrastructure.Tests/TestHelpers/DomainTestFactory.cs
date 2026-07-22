using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Infrastructure.Tests.TestHelpers;

/// <summary>Shared builders for round-trip repository tests.</summary>
internal static class DomainTestFactory
{
    public static readonly GeoCoordinate RestaurantLocation = new(52.2297, 21.0122);
    public static readonly GeoCoordinate NearbyPoint = new(52.2310, 21.0130);

    public static DomainRestaurant CreateRestaurant()
    {
        // Open every day, effectively around the clock: these are persistence round-trip tests,
        // not opening-hours business rules (covered separately in Domain.Tests) — Order.Create's
        // placed-at time is real DateTimeOffset.UtcNow, so a schedule with gaps (e.g. only
        // Monday/Tuesday) makes these tests fail depending on which day they happen to run.
        var allDayEveryDay = new List<TimeRange> { new(new TimeOnly(0, 0), new TimeOnly(23, 59)) };
        var schedule = Enum.GetValues<DayOfWeek>()
            .ToDictionary(day => day, IReadOnlyList<TimeRange> (_) => allDayEveryDay);

        return DomainRestaurant.Create(
            "Pizza Palace",
            new Address("Main St", "1", "Warsaw", "00-001", "3B", "Ring the bell"),
            RestaurantLocation,
            deliveryRadiusKm: 5,
            timeZoneId: "Europe/Warsaw",
            openingHours: new OpeningHours(schedule),
            contactPhone: "123456789",
            deliveryFee: new Money(10),
            minimumOrderValue: new Money(20),
            freeDeliveryThreshold: new Money(80));
    }

    public static Ingredient CreateIngredient(string name = "Cheese", decimal price = 2m) =>
        Ingredient.Create(name, new Money(price), "Dairy");

    public static MenuItem CreatePizzaWithVariantsAndIngredients(
        Ingredient baseIngredient,
        Ingredient extraIngredient)
    {
        var pizza = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(25), description: "Classic");
        pizza.AddBaseIngredient(baseIngredient);
        pizza.AllowExtra(extraIngredient);
        pizza.AddVariant(MenuItemVariant.Create("Small", new Money(20), isDefault: true));
        pizza.AddVariant(MenuItemVariant.Create("Large", new Money(30)));
        return pizza;
    }

    public static ContactDetails SampleContact() => new("Jan Kowalski", "123456789", "jan@example.com");

    public static DeliveryAddress SampleDeliveryAddress() =>
        new(new Address("Client St", "2", "Warsaw", "00-002"), NearbyPoint);

    public static Order CreateDeliveryOrderWithExtras(DomainRestaurant restaurant, Guid ingredientId)
    {
        var extra = new OrderItemExtra(ingredientId, "Extra cheese", new Money(3));
        var item = OrderItem.Create(
            Guid.NewGuid(),
            "Margherita",
            new Money(25),
            quantity: 2,
            variantId: Guid.NewGuid(),
            variantName: "Large",
            extras: new[] { extra },
            notes: "No onions");

        return Order.Create(
            "20260720-0001",
            customerId: null,
            SampleContact(),
            FulfillmentType.Delivery,
            SampleDeliveryAddress(),
            new[] { item },
            DateTimeOffset.UtcNow,
            requestedFulfillmentTime: null,
            PaymentMethod.Online,
            restaurant);
    }

    /// <summary>
    /// Pickup order with no <see cref="DeliveryAddress"/> at all — the scenario that
    /// justifies the <c>HasDeliveryAddress</c> presence-marker shadow property on
    /// <c>OwnedDeliveryAddress</c> (distinguishing "no DeliveryAddress" from "an instance
    /// whose nested columns all happen to be null").
    /// </summary>
    public static Order CreatePickupOrder(DomainRestaurant restaurant, string orderNumber = "20260720-0002")
    {
        var pizza = MenuItem.Create("Margherita", MenuCategory.Pizza, new Money(25));
        var item = OrderItem.Create(pizza.Id, pizza.Name, pizza.BasePrice, quantity: 1);

        return Order.Create(
            orderNumber,
            customerId: null,
            SampleContact(),
            FulfillmentType.Pickup,
            deliveryAddress: null,
            new[] { item },
            DateTimeOffset.UtcNow,
            requestedFulfillmentTime: null,
            PaymentMethod.OnPickup,
            restaurant);
    }

    public static Customer CreateCustomer() =>
        Customer.Create(
            userAccountId: Guid.NewGuid(),
            fullName: "Anna Nowak",
            email: "anna@example.com",
            createdAt: DateTimeOffset.UtcNow,
            phoneNumber: "500600700");

    public static LoyaltyAccount CreateLoyaltyAccountWithHistory(Guid customerId)
    {
        var account = LoyaltyAccount.Create(customerId);
        account.Earn(100, "Order completed", DateTimeOffset.UtcNow, Guid.NewGuid());
        account.Redeem(20, "Redeemed on next order", DateTimeOffset.UtcNow);
        return account;
    }

    public static Promotion CreatePromotion() =>
        Promotion.Create(
            "Summer sale",
            PromotionType.Percentage,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            value: 10m,
            code: "SUMMER10",
            minOrderValue: new Money(15),
            usageLimit: 100);
}
