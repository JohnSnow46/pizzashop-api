namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a delivery address falls outside the restaurant's delivery radius
/// (domain-model.md 5.4 rule 3, ADR-0006).
/// </summary>
public sealed class AddressOutsideDeliveryAreaException : DomainException
{
    public AddressOutsideDeliveryAreaException(double distanceKm, double deliveryRadiusKm)
        : base($"Delivery address is {distanceKm:F2} km away, which exceeds the delivery radius of {deliveryRadiusKm:F2} km.")
    {
    }
}
