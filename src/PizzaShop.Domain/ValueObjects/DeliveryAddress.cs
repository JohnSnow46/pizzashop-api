namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Composition of a postal <see cref="Address"/> and its <see cref="GeoCoordinate"/>,
/// required to validate the delivery radius (domain-model.md 2.4, ADR-0006). Used as a
/// snapshot VO on <c>Order</c>; wrapped by a child entity in the customer's address book.
/// </summary>
public sealed class DeliveryAddress : IEquatable<DeliveryAddress>
{
    public Address Address { get; }
    public GeoCoordinate Coordinate { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private DeliveryAddress()
    {
    }

    public DeliveryAddress(Address address, GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(coordinate);

        Address = address;
        Coordinate = coordinate;
    }

    public bool Equals(DeliveryAddress? other) =>
        other is not null && Address == other.Address && Coordinate == other.Coordinate;

    public override bool Equals(object? obj) => Equals(obj as DeliveryAddress);

    public override int GetHashCode() => HashCode.Combine(Address, Coordinate);

    public static bool operator ==(DeliveryAddress? left, DeliveryAddress? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(DeliveryAddress? left, DeliveryAddress? right) => !(left == right);
}
