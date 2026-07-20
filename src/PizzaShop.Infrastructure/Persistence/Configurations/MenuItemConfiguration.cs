using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="MenuItem"/> aggregate — owned <see cref="MenuItemVariant"/>
/// collection plus two independent many-to-many relations to the shared
/// <see cref="Ingredient"/> dictionary (<c>BaseIngredients</c>, <c>AllowedExtras</c>), the
/// hardest part of the catalog mapping (ADR-0020, infrastructure-layer.md 2.3).
/// </summary>
public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.ImageUrl).HasMaxLength(500);
        builder.Property(m => m.IsAvailable).IsRequired();

        builder.OwnsMany(m => m.Variants, variant =>
        {
            variant.ToTable("MenuItemVariants");
            variant.WithOwner().HasForeignKey("MenuItemId");
            variant.HasKey(v => v.Id);
            variant.Property(v => v.Id).ValueGeneratedNever();

            variant.Property(v => v.Name).HasMaxLength(100).IsRequired();
            variant.Property(v => v.IsDefault).IsRequired();
        });
        builder.Navigation(m => m.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Two independent many-to-many relations to the same Ingredient dictionary, each
        // with its own explicitly named join table (infrastructure-layer.md 2.3).
        builder.HasMany(m => m.BaseIngredients)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "MenuItemBaseIngredients",
                right => right.HasOne<Ingredient>().WithMany().HasForeignKey("IngredientId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<MenuItem>().WithMany().HasForeignKey("MenuItemId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("MenuItemBaseIngredients");
                    join.HasKey("MenuItemId", "IngredientId");
                });
        builder.Navigation(m => m.BaseIngredients).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.AllowedExtras)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "MenuItemAllowedExtras",
                right => right.HasOne<Ingredient>().WithMany().HasForeignKey("IngredientId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<MenuItem>().WithMany().HasForeignKey("MenuItemId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("MenuItemAllowedExtras");
                    join.HasKey("MenuItemId", "IngredientId");
                });
        builder.Navigation(m => m.AllowedExtras).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
