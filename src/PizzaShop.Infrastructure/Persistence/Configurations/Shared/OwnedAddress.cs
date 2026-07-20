using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Configurations.Shared;

/// <summary>
/// Column mapping for the <see cref="Address"/> Value Object, reused everywhere it is owned
/// (<c>Restaurant.Address</c>, nested inside <see cref="DeliveryAddress"/>) — infrastructure-
/// layer.md 2.2/3.
/// </summary>
internal static class OwnedAddress
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, Address> builder)
        where TOwner : class
    {
        builder.Property(a => a.Street).HasMaxLength(200).IsRequired();
        builder.Property(a => a.BuildingNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.ApartmentNumber).HasMaxLength(20);
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
    }
}
