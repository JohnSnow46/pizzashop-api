using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Review"/> aggregate (ADR-0042, ADR-0020).
/// </summary>
public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.MenuItemId).IsRequired();
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(Review.MaxCommentLength);
        builder.Property(r => r.CreatedAt).IsRequired();

        // One review per (order, menu item) — ADR-0042 decision (B); the handler also checks
        // this before insert, this index is the second line of defense against a race.
        builder.HasIndex(r => new { r.OrderId, r.MenuItemId }).IsUnique();
        builder.HasIndex(r => r.MenuItemId);
        builder.HasIndex(r => r.CustomerId);
    }
}
