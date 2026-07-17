using FluentAssertions;
using PizzaShop.Domain.Customers;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Tests.Customers;

public class CustomerTests
{
    private static readonly DateTimeOffset CreatedAt = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Customer CreateCustomer() =>
        Customer.Create(Guid.NewGuid(), "Jan Kowalski", "jan@example.com", Guid.NewGuid(), CreatedAt);

    private static DeliveryAddress SampleAddress() =>
        new(new Address("Main St", "1", "Warsaw", "00-001"), new GeoCoordinate(52.0, 21.0));

    [Fact]
    public void Create_EmptyUserAccountId_ThrowsArgumentException()
    {
        var act = () => Customer.Create(Guid.Empty, "Jan Kowalski", "jan@example.com", Guid.NewGuid(), CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_MissingFullName_ThrowsArgumentException()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "", "jan@example.com", Guid.NewGuid(), CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_MissingEmail_ThrowsArgumentException()
    {
        var act = () => Customer.Create(Guid.NewGuid(), "Jan Kowalski", "", Guid.NewGuid(), CreatedAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddAddress_FirstAddressMarkedDefault_IsDefaultTrue()
    {
        var customer = CreateCustomer();

        var entry = customer.AddAddress("Home", SampleAddress(), isDefault: true);

        entry.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddAddress_SecondDefaultAddress_UnsetsPreviousDefault()
    {
        var customer = CreateCustomer();
        var home = customer.AddAddress("Home", SampleAddress(), isDefault: true);

        var work = customer.AddAddress("Work", SampleAddress(), isDefault: true);

        home.IsDefault.Should().BeFalse();
        work.IsDefault.Should().BeTrue();
        customer.AddressBook.Count(a => a.IsDefault).Should().Be(1);
    }

    [Fact]
    public void RemoveAddress_ExistingAddress_RemovesFromAddressBook()
    {
        var customer = CreateCustomer();
        var entry = customer.AddAddress("Home", SampleAddress());

        customer.RemoveAddress(entry.Id);

        customer.AddressBook.Should().BeEmpty();
    }

    [Fact]
    public void SetDefaultAddress_UnknownAddressId_ThrowsAddressNotInAddressBookException()
    {
        var customer = CreateCustomer();

        var act = () => customer.SetDefaultAddress(Guid.NewGuid());

        act.Should().Throw<AddressNotInAddressBookException>();
    }

    [Fact]
    public void SetDefaultAddress_KnownAddressId_MarksItDefaultAndOthersNot()
    {
        var customer = CreateCustomer();
        var home = customer.AddAddress("Home", SampleAddress(), isDefault: true);
        var work = customer.AddAddress("Work", SampleAddress());

        customer.SetDefaultAddress(work.Id);

        home.IsDefault.Should().BeFalse();
        work.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UpdateContactDetails_MissingFullName_ThrowsArgumentException()
    {
        var customer = CreateCustomer();

        var act = () => customer.UpdateContactDetails("", "123456789");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContactDetails_ValidValues_UpdatesFields()
    {
        var customer = CreateCustomer();

        customer.UpdateContactDetails("Jan Nowak", "987654321");

        customer.FullName.Should().Be("Jan Nowak");
        customer.PhoneNumber.Should().Be("987654321");
    }
}
