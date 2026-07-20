using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Configurations.Shared;

/// <summary>
/// Column mapping for the <see cref="DeliveryAddress"/> composite Value Object — a nested
/// owned <see cref="Address"/> plus <see cref="GeoCoordinate"/> (ADR-0020, infrastructure-
/// layer.md 2.2). Optional on <c>Order</c>, required on <c>CustomerAddress</c>.
/// </summary>
internal static class OwnedDeliveryAddress
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, DeliveryAddress> builder)
        where TOwner : class
    {
        builder.OwnsOne(d => d.Address, OwnedAddress.Configure);
        builder.Navigation(d => d.Address).IsRequired();

        builder.OwnsOne(d => d.Coordinate, OwnedGeoCoordinate.Configure);
        builder.Navigation(d => d.Coordinate).IsRequired();

        // Presence marker required by EF for optional owned types that only contain further
        // nested owned types (e.g. Order.DeliveryAddress) and no scalar property of their
        // own — without it, EF cannot tell "instance is null" from "every nested column is
        // null" when sharing the owner's table. Harmless no-op on required usages
        // (CustomerAddress.DeliveryAddress).
        builder.Property<bool>("HasDeliveryAddress").IsRequired();
    }
}
