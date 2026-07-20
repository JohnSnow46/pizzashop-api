namespace PizzaShop.Domain.ValueObjects;

/// <summary>
/// Geographic coordinate with Haversine great-circle distance (domain-model.md 2.2,
/// ADR-0006).
/// </summary>
public sealed class GeoCoordinate : IEquatable<GeoCoordinate>
{
    private const double EarthRadiusKm = 6371.0;

    public double Latitude { get; }
    public double Longitude { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private GeoCoordinate()
    {
    }

    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public double DistanceKmTo(GeoCoordinate other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var lat1Rad = ToRadians(Latitude);
        var lat2Rad = ToRadians(other.Latitude);
        var deltaLatRad = ToRadians(other.Latitude - Latitude);
        var deltaLonRad = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public bool Equals(GeoCoordinate? other) =>
        other is not null && Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

    public override bool Equals(object? obj) => Equals(obj as GeoCoordinate);

    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

    public override string ToString() => $"({Latitude}, {Longitude})";

    public static bool operator ==(GeoCoordinate? left, GeoCoordinate? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(GeoCoordinate? left, GeoCoordinate? right) => !(left == right);
}
