using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Configurations.Shared;

/// <summary>
/// Column mapping for the <see cref="GeoCoordinate"/> Value Object — two plain columns, the
/// distance calculation stays in Domain (ADR-0006, infrastructure-layer.md 2.2).
/// </summary>
internal static class OwnedGeoCoordinate
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, GeoCoordinate> builder)
        where TOwner : class
    {
        builder.Property(g => g.Latitude).IsRequired();
        builder.Property(g => g.Longitude).IsRequired();
    }
}
