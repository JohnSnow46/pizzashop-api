using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Loyalty;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="LoyaltyAccount"/> aggregate — append-only
/// <see cref="LoyaltyTransaction"/> history as an owned collection (ADR-0009, ADR-0020).
/// </summary>
public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("LoyaltyAccounts");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.HasIndex(l => l.CustomerId).IsUnique();
        builder.Property(l => l.PointsBalance).IsRequired();

        // Optimistic concurrency via Postgres's native xmin system column (ADR-0040) — guards
        // against a lost update when two concurrent orders redeem points from the same
        // account. Zero schema changes; a concurrent write conflict surfaces as
        // DbUpdateConcurrencyException, mapped to 409 by ExceptionHandler.
        builder.UseXminAsConcurrencyToken();

        builder.OwnsMany(l => l.Transactions, transaction =>
        {
            transaction.ToTable("LoyaltyTransactions");
            transaction.WithOwner().HasForeignKey("LoyaltyAccountId");
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Id).ValueGeneratedNever();

            transaction.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            transaction.Property(t => t.Points).IsRequired();
            transaction.Property(t => t.Reason).HasMaxLength(500).IsRequired();
            transaction.Property(t => t.OccurredAt).IsRequired();
        });
        builder.Navigation(l => l.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
