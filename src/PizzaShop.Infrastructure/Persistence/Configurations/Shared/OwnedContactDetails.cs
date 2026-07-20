using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Infrastructure.Persistence.Configurations.Shared;

/// <summary>
/// Column mapping for the <see cref="ContactDetails"/> Value Object, always present on
/// <c>Order</c> (infrastructure-layer.md 2.2).
/// </summary>
internal static class OwnedContactDetails
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, ContactDetails> builder)
        where TOwner : class
    {
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
    }
}
