using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Abstractions.Geocoding;

/// <summary>
/// Resolves the geographic coordinates of a postal address (ADR-0006), used to validate
/// delivery-radius eligibility. Returns <c>null</c> when the address cannot be geocoded —
/// callers treat that as "delivery not possible", not as an infrastructure error.
/// </summary>
public interface IGeocodingService
{
    Task<GeoCoordinate?> GeocodeAsync(Address address, CancellationToken cancellationToken);
}
