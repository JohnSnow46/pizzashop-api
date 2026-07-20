namespace PizzaShop.Infrastructure.Geocoding;

/// <summary>
/// Configuration for <see cref="NominatimGeocodingService"/> (ADR-0023).
/// </summary>
public sealed class GeocodingOptions
{
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Required by Nominatim's usage policy — requests without a descriptive User-Agent are
    /// blocked (infrastructure-layer.md 6).
    /// </summary>
    public string UserAgent { get; set; } = "PizzaShop/1.0";

    public int TimeoutSeconds { get; set; } = 10;
}
