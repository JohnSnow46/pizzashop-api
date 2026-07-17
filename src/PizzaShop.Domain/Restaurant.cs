using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain;

/// <summary>
/// Single-tenant restaurant configuration (domain-model.md 3, ADR-0003): location,
/// opening hours, delivery area and ordering thresholds.
/// </summary>
public class Restaurant
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Address Address { get; private set; }
    public GeoCoordinate Location { get; private set; }
    public double DeliveryRadiusKm { get; private set; }
    public string TimeZoneId { get; private set; }
    public OpeningHours OpeningHours { get; private set; }
    public string ContactPhone { get; private set; }
    public bool IsAcceptingOrders { get; private set; }
    public Money? MinimumOrderValue { get; private set; }
    public Money? FreeDeliveryThreshold { get; private set; }
    public Money DeliveryFee { get; private set; }

    private Restaurant(
        Guid id,
        string name,
        Address address,
        GeoCoordinate location,
        double deliveryRadiusKm,
        string timeZoneId,
        OpeningHours openingHours,
        string contactPhone,
        Money deliveryFee)
    {
        Id = id;
        Name = name;
        Address = address;
        Location = location;
        DeliveryRadiusKm = deliveryRadiusKm;
        TimeZoneId = timeZoneId;
        OpeningHours = openingHours;
        ContactPhone = contactPhone;
        DeliveryFee = deliveryFee;
        IsAcceptingOrders = true;
    }

    public static Restaurant Create(
        string name,
        Address address,
        GeoCoordinate location,
        double deliveryRadiusKm,
        string timeZoneId,
        OpeningHours openingHours,
        string contactPhone,
        Money deliveryFee,
        Money? minimumOrderValue = null,
        Money? freeDeliveryThreshold = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(location);
        if (deliveryRadiusKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(deliveryRadiusKm), "Delivery radius must be greater than zero.");
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone id is required.", nameof(timeZoneId));
        ArgumentNullException.ThrowIfNull(openingHours);
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ArgumentException("Contact phone is required.", nameof(contactPhone));
        ArgumentNullException.ThrowIfNull(deliveryFee);

        var restaurant = new Restaurant(
            Guid.NewGuid(),
            name,
            address,
            location,
            deliveryRadiusKm,
            timeZoneId,
            openingHours,
            contactPhone,
            deliveryFee)
        {
            MinimumOrderValue = minimumOrderValue,
            FreeDeliveryThreshold = freeDeliveryThreshold,
        };

        return restaurant;
    }

    public bool IsWithinDeliveryArea(GeoCoordinate point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return Location.DistanceKmTo(point) <= DeliveryRadiusKm;
    }

    public bool CanAcceptOrderAt(DateTimeOffset when)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return IsAcceptingOrders && OpeningHours.IsOpenAt(when, timeZone);
    }

    public void StartAcceptingOrders() => IsAcceptingOrders = true;

    public void StopAcceptingOrders() => IsAcceptingOrders = false;

    public void UpdateDeliveryArea(GeoCoordinate location, double deliveryRadiusKm)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (deliveryRadiusKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(deliveryRadiusKm), "Delivery radius must be greater than zero.");

        Location = location;
        DeliveryRadiusKm = deliveryRadiusKm;
    }

    public void UpdateOpeningHours(OpeningHours openingHours)
    {
        ArgumentNullException.ThrowIfNull(openingHours);
        OpeningHours = openingHours;
    }

    public void UpdateOrderingThresholds(Money? minimumOrderValue, Money? freeDeliveryThreshold, Money deliveryFee)
    {
        ArgumentNullException.ThrowIfNull(deliveryFee);
        MinimumOrderValue = minimumOrderValue;
        FreeDeliveryThreshold = freeDeliveryThreshold;
        DeliveryFee = deliveryFee;
    }
}
