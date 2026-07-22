using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Round-trip coverage for <see cref="CustomerRepository"/> — the owned <c>AddressBook</c>
/// collection, each entry wrapping a required, doubly-nested <c>DeliveryAddress</c>
/// (ADR-0020).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CustomerRepositoryTests : PostgresRepositoryTestBase
{
    public CustomerRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAndGet_RoundTripsAddressBookWithNestedDeliveryAddress()
    {
        var customer = DomainTestFactory.CreateCustomer();
        var homeAddress = customer.AddAddress("Home", DomainTestFactory.SampleDeliveryAddress(), isDefault: true);

        await using (var writeContext = Fixture.CreateContext())
        {
            var repository = new CustomerRepository(writeContext);
            await repository.AddAsync(customer, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new CustomerRepository(readContext);

        var loaded = await readRepository.GetByUserAccountIdAsync(customer.UserAccountId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.AddressBook.Should().HaveCount(1);

        var loadedAddress = loaded.AddressBook.Single();
        loadedAddress.Id.Should().Be(homeAddress.Id);
        loadedAddress.IsDefault.Should().BeTrue();
        loadedAddress.DeliveryAddress.Should().Be(homeAddress.DeliveryAddress);
    }
}
