using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// Deterministic <see cref="IGeocodingService"/> for tests — the real implementation
/// (<c>NominatimGeocodingService</c>, Infrastructure) calls an external HTTP service, which
/// this project deliberately never does (see <see cref="InMemoryUserAccountRepository"/> for
/// the same rationale applied to persistence). Resolves any <see cref="Address"/> to
/// <see cref="InMemoryRestaurantRepository"/>'s own location (distance 0, always within its
/// delivery radius), except two sentinel streets tests use to exercise the other branches:
/// <see cref="OutsideRadiusStreet"/> (geocodes far away, outside the radius) and
/// <see cref="UngeocodableStreet"/> (cannot be geocoded at all, mirrors an unknown/malformed
/// address).
/// </summary>
public sealed class FakeGeocodingService : IGeocodingService
{
    public const string OutsideRadiusStreet = "OutsideRadiusStreet";
    public const string UngeocodableStreet = "UngeocodableStreet";

    private static readonly GeoCoordinate RestaurantCoordinate = new(50.0614, 19.9366);
    private static readonly GeoCoordinate FarAwayCoordinate = new(52.2297, 21.0122); // Warsaw — well outside a 5 km Kraków radius.

    public Task<GeoCoordinate?> GeocodeAsync(Address address, CancellationToken cancellationToken)
    {
        GeoCoordinate? coordinate = address.Street switch
        {
            OutsideRadiusStreet => FarAwayCoordinate,
            UngeocodableStreet => null,
            _ => RestaurantCoordinate,
        };

        return Task.FromResult(coordinate);
    }
}
