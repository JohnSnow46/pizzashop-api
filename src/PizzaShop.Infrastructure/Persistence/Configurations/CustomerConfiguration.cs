using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PizzaShop.Domain.Customers;
using PizzaShop.Infrastructure.Persistence.Configurations.Shared;

namespace PizzaShop.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Customer"/> aggregate — owned <see cref="CustomerAddress"/>
/// address book, each entry wrapping a required <see cref="Domain.ValueObjects.DeliveryAddress"/>
/// (ADR-0020, infrastructure-layer.md 2.3).
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.HasIndex(c => c.UserAccountId).IsUnique();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.LoyaltyAccountId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.OwnsMany(c => c.AddressBook, address =>
        {
            address.ToTable("CustomerAddresses");
            address.WithOwner().HasForeignKey("CustomerId");
            address.HasKey(a => a.Id);
            address.Property(a => a.Id).ValueGeneratedNever();

            address.Property(a => a.Label).HasMaxLength(100).IsRequired();
            address.Property(a => a.IsDefault).IsRequired();

            address.OwnsOne(a => a.DeliveryAddress, OwnedDeliveryAddress.Configure);
            address.Navigation(a => a.DeliveryAddress).IsRequired();
        });
        builder.Navigation(c => c.AddressBook).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
