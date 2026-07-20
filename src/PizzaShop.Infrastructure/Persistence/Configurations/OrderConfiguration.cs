using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Orders;
using PizzaShop.Infrastructure.Persistence.Configurations.Shared;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Order"/> aggregate — owned <see cref="ContactDetails"/> and
/// optional <see cref="DeliveryAddress"/>, owned <see cref="OrderItem"/> collection with a
/// nested owned <c>Extras</c> collection, plus the <c>GuestTrackingToken</c>/
/// <c>ProviderPaymentReference</c> sidecar shadow properties (ADR-0018, ADR-0021, ADR-0020).
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.HasIndex(o => o.Number).IsUnique();
        builder.Property(o => o.Number).HasMaxLength(50).IsRequired();

        builder.Property(o => o.FulfillmentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(o => o.PlacedAt).IsRequired();
        builder.Property(o => o.RequestedFulfillmentTime);
        builder.Property(o => o.EstimatedReadyAt);
        builder.Property(o => o.PointsToEarn).IsRequired();
        builder.Property(o => o.PointsRedeemed).IsRequired();

        builder.OwnsOne(o => o.Contact, OwnedContactDetails.Configure);
        builder.Navigation(o => o.Contact).IsRequired();

        // Optional — only present for Delivery orders (domain-model.md 5.4 rule 1).
        builder.OwnsOne(o => o.DeliveryAddress, OwnedDeliveryAddress.Configure);

        builder.OwnsMany(o => o.Items, item =>
        {
            item.ToTable("OrderItems");
            item.WithOwner().HasForeignKey("OrderId");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever();

            item.Property(i => i.MenuItemId).IsRequired();
            item.Property(i => i.MenuItemName).HasMaxLength(200).IsRequired();
            item.Property(i => i.VariantId);
            item.Property(i => i.VariantName).HasMaxLength(100);
            item.Property(i => i.Quantity).IsRequired();
            item.Property(i => i.Notes).HasMaxLength(500);

            item.OwnsMany(i => i.Extras, extra =>
            {
                extra.ToTable("OrderItemExtras");
                extra.WithOwner().HasForeignKey("OrderItemId");

                // OrderItemExtra is a plain snapshot Value Object with no natural id
                // (infrastructure-layer.md 2.2) — a generated shadow key is the simplest way
                // to give the owned collection row identity for EF Core.
                extra.Property<int>("Id").ValueGeneratedOnAdd();
                extra.HasKey("Id");

                extra.Property(e => e.IngredientId).IsRequired();
                extra.Property(e => e.Name).HasMaxLength(200).IsRequired();
            });
            item.Navigation(i => i.Extras).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Sidecar data kept outside Domain (ADR-0018/ADR-0021) — shadow properties on the
        // same table, accessed exclusively through OrderRepository.
        builder.Property<Guid?>("GuestTrackingToken");
        builder.HasIndex("GuestTrackingToken").IsUnique();

        builder.Property<string?>("ProviderPaymentReference").HasMaxLength(100);
    }
}
