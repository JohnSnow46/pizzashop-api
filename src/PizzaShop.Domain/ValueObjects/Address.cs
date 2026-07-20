namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Purely postal address, without geolocation or identity (domain-model.md 2.3).
/// </summary>
public sealed class Address : IEquatable<Address>
{
    public string Street { get; }
    public string BuildingNumber { get; }
    public string? ApartmentNumber { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string? Notes { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Address()
    {
    }

    public Address(
        string street,
        string buildingNumber,
        string city,
        string postalCode,
        string? apartmentNumber = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(buildingNumber))
            throw new ArgumentException("Building number is required.", nameof(buildingNumber));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));

        Street = street;
        BuildingNumber = buildingNumber;
        City = city;
        PostalCode = postalCode;
        ApartmentNumber = apartmentNumber;
        Notes = notes;
    }

    public bool Equals(Address? other) =>
        other is not null
        && Street == other.Street
        && BuildingNumber == other.BuildingNumber
        && ApartmentNumber == other.ApartmentNumber
        && City == other.City
        && PostalCode == other.PostalCode
        && Notes == other.Notes;

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode() =>
        HashCode.Combine(Street, BuildingNumber, ApartmentNumber, City, PostalCode);

    public override string ToString() =>
        $"{Street} {BuildingNumber}{(ApartmentNumber is null ? "" : $"/{ApartmentNumber}")}, {PostalCode} {City}";

    public static bool operator ==(Address? left, Address? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Address? left, Address? right) => !(left == right);
}
