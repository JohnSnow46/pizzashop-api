using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Infrastructure.Persistence.Converters;
using PizzaShop.Infrastructure.Persistence.Configurations.Shared;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the single <see cref="DomainRestaurant"/> record (ADR-0015, ADR-0020).
/// </summary>
public sealed class RestaurantConfiguration : IEntityTypeConfiguration<DomainRestaurant>
{
    public void Configure(EntityTypeBuilder<DomainRestaurant> builder)
    {
        builder.ToTable("Restaurants");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ContactPhone).HasMaxLength(30).IsRequired();
        builder.Property(r => r.DeliveryRadiusKm).IsRequired();
        builder.Property(r => r.IsAcceptingOrders).IsRequired();

        builder.OwnsOne(r => r.Address, OwnedAddress.Configure);
        builder.Navigation(r => r.Address).IsRequired();

        builder.OwnsOne(r => r.Location, OwnedGeoCoordinate.Configure);
        builder.Navigation(r => r.Location).IsRequired();

        builder.Property(r => r.OpeningHours)
            .HasConversion(new OpeningHoursConverter(), new OpeningHoursValueComparer())
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
