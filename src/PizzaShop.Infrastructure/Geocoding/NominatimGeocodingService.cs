using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Geocoding;

/// <summary>
/// <see cref="IGeocodingService"/> backed by OpenStreetMap's Nominatim search API (ADR-0023) —
/// free, no API key, sufficient for a single low-volume pizzeria. Returns <c>null</c> when the
/// address cannot be resolved, which callers treat as "delivery not possible" rather than an
/// infrastructure error.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    public NominatimGeocodingService(HttpClient httpClient, IOptions<GeocodingOptions> options)
    {
        _httpClient = httpClient;

        var geocodingOptions = options.Value;
        _httpClient.BaseAddress ??= new Uri(geocodingOptions.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(geocodingOptions.TimeoutSeconds);

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(geocodingOptions.UserAgent);
    }

    public async Task<GeoCoordinate?> GeocodeAsync(Address address, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);

        var street = Uri.EscapeDataString($"{address.BuildingNumber} {address.Street}");
        var city = Uri.EscapeDataString(address.City);
        var postalCode = Uri.EscapeDataString(address.PostalCode);

        var requestUri =
            $"/search?format=jsonv2&street={street}&city={city}&postalcode={postalCode}&country=Poland&limit=1";

        var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(requestUri, cancellationToken);
        var first = results?.FirstOrDefault();

        if (first is null)
            return null;

        return new GeoCoordinate(
            double.Parse(first.Lat, CultureInfo.InvariantCulture),
            double.Parse(first.Lon, CultureInfo.InvariantCulture));
    }

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);
}
