using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Application.Identity;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for <see cref="PasswordResetToken"/> — like <see cref="UserAccountConfiguration"/>,
/// a non-Domain entity (identity lives outside Domain, ADR-0005). <c>Token</c> carries a unique
/// index (lookup key); <c>UserAccountId</c> carries a lookup index (filtered by the repository
/// when invalidating superseded tokens).
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.HasIndex(t => t.Token).IsUnique();
        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();

        builder.Property(t => t.UserAccountId).IsRequired();
        builder.HasIndex(t => t.UserAccountId);

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.UsedAt);
    }
}
