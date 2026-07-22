using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Promotion"/> aggregate (ADR-0019, ADR-0020). Owned
/// <see cref="Domain.Promotions.BuyXGetYRule"/> (nullable — present iff <c>Type == BuyXGetY</c>,
/// domain-model.md 8.2, ADR-0034).
/// </summary>
public sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(p => p.Value).HasPrecision(12, 2);

        builder.Property(p => p.ValidFrom).IsRequired();
        builder.Property(p => p.ValidTo).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.UsageCount).IsRequired();

        builder.OwnsOne(p => p.BuyXGetYRule, rule =>
        {
            rule.Property(r => r.TriggerMenuItemId).HasColumnName("BuyXGetY_TriggerMenuItemId");
            rule.Property(r => r.BuyQuantity).HasColumnName("BuyXGetY_BuyQuantity");
            rule.Property(r => r.RewardMenuItemId).HasColumnName("BuyXGetY_RewardMenuItemId");
            rule.Property(r => r.GetQuantity).HasColumnName("BuyXGetY_GetQuantity");
            rule.Property(r => r.RewardDiscountPercentage).HasColumnName("BuyXGetY_RewardDiscountPercentage").HasPrecision(5, 2);
        });
    }
}
