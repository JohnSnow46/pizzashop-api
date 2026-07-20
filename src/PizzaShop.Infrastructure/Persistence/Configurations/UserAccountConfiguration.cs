using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Application.Identity;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for <see cref="UserAccount"/> — the only non-Domain entity in
/// <see cref="PizzaShopDbContext"/> (identity deliberately lives outside Domain, ADR-0005),
/// added by ADR-0026. <c>Email</c> carries a unique index; <c>Role</c> is stored as its enum
/// name, consistent with every other enum in this schema (infrastructure-layer.md 2.2).
/// </summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.HasIndex(a => a.Email).IsUnique();
        builder.Property(a => a.Email).HasMaxLength(200).IsRequired();

        builder.Property(a => a.PasswordHash).HasMaxLength(200).IsRequired();

        builder.Property(a => a.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
    }
}
