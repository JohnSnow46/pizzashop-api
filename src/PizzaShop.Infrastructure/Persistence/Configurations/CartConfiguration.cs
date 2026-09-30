using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Carts;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Cart"/> aggregate — owned <see cref="CartItem"/> collection
/// (ADR-0043, ADR-0020). <c>ExtraIds</c> has no per-row data of its own (unlike
/// <c>OrderItemExtra</c>, which snapshots a price/name), so it is stored as a delimited
/// string on the <c>CartItems</c> row instead of a third owned table.
/// </summary>
public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.HasIndex(c => c.CustomerId).IsUnique();
        builder.Property(c => c.CustomerId).IsRequired();

        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.OwnsMany(c => c.Items, item =>
        {
            item.ToTable("CartItems");
            item.WithOwner().HasForeignKey("CartId");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever();

            item.Property(i => i.MenuItemId).IsRequired();
            item.Property(i => i.VariantId);
            item.Property(i => i.Quantity).IsRequired();
            item.Property(i => i.Notes).HasMaxLength(500);

            var extraIdsComparer = new ValueComparer<IReadOnlyList<Guid>>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
                v => v.ToArray());

            item.Property(i => i.ExtraIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => string.IsNullOrEmpty(v)
                        ? Array.Empty<Guid>()
                        : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray())
                .Metadata.SetValueComparer(extraIdsComparer);
        });
        builder.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
