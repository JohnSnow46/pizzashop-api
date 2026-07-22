using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;
using PizzaShop.Infrastructure.Time;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Round-trip coverage for <see cref="OrderRepository"/>: nested owned <c>Items</c> →
/// <c>Extras</c>, the optional owned <c>DeliveryAddress</c>, and the
/// <c>GuestTrackingToken</c>/<c>ProviderPaymentReference</c> sidecar shadow properties
/// (ADR-0018, ADR-0021).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class OrderRepositoryTests : PostgresRepositoryTestBase
{
    public OrderRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddAndGetByGuestTrackingToken_RoundTripsItemsExtrasAndSidecarData()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();
        var ingredientId = Guid.NewGuid();
        var order = DomainTestFactory.CreateDeliveryOrderWithExtras(restaurant, ingredientId);
        var guestTrackingToken = Guid.NewGuid();
        var providerPaymentReference = "PAYU-12345";

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Restaurants.AddAsync(restaurant);

            var repository = new OrderRepository(writeContext, new SystemClock());
            await repository.AddAsync(order, guestTrackingToken, providerPaymentReference, CancellationToken.None);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new OrderRepository(readContext, new SystemClock());

        var loaded = await readRepository.GetByGuestTrackingTokenAsync(guestTrackingToken, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(order.Id);
        loaded.DeliveryAddress.Should().Be(order.DeliveryAddress);
        loaded.Contact.Should().Be(order.Contact);
        loaded.Items.Should().HaveCount(1);

        var loadedItem = loaded.Items.Single();
        loadedItem.Quantity.Should().Be(2);
        loadedItem.Extras.Should().HaveCount(1);
        loadedItem.Extras.Single().IngredientId.Should().Be(ingredientId);
        loadedItem.LineTotal.Should().Be(order.Items.Single().LineTotal);

        var storedReference = await readRepository.GetProviderPaymentReferenceAsync(order.Id, CancellationToken.None);
        storedReference.Should().Be(providerPaymentReference);
    }

    [Fact]
    public async Task AddAndGet_PickupOrderWithoutDeliveryAddress_RoundTripsNullDeliveryAddress()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();
        var order = DomainTestFactory.CreatePickupOrder(restaurant);

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Restaurants.AddAsync(restaurant);

            var repository = new OrderRepository(writeContext, new SystemClock());
            await repository.AddAsync(order, guestTrackingToken: null, providerPaymentReference: null, CancellationToken.None);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new OrderRepository(readContext, new SystemClock());

        var loaded = await readRepository.GetByIdAsync(order.Id, CancellationToken.None);

        // Exercises the "HasDeliveryAddress" presence marker (OwnedDeliveryAddress) — without
        // it EF cannot tell "DeliveryAddress is null" apart from "an instance whose nested
        // Address/Coordinate columns all happen to be null" when sharing the Orders table.
        loaded.Should().NotBeNull();
        loaded!.DeliveryAddress.Should().BeNull();
        loaded.Contact.Should().Be(order.Contact);
        loaded.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetProviderPaymentReference_OverwritesPreviousValue()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();
        var order = DomainTestFactory.CreateDeliveryOrderWithExtras(restaurant, Guid.NewGuid());

        await using (var writeContext = Fixture.CreateContext())
        {
            await writeContext.Restaurants.AddAsync(restaurant);
            var repository = new OrderRepository(writeContext, new SystemClock());
            await repository.AddAsync(order, guestTrackingToken: null, providerPaymentReference: null, CancellationToken.None);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = Fixture.CreateContext())
        {
            var repository = new OrderRepository(updateContext, new SystemClock());
            await repository.SetProviderPaymentReferenceAsync(order.Id, "PAYU-99999", CancellationToken.None);
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = Fixture.CreateContext();
        var readRepository = new OrderRepository(readContext, new SystemClock());
        var reference = await readRepository.GetProviderPaymentReferenceAsync(order.Id, CancellationToken.None);

        reference.Should().Be("PAYU-99999");
    }

    [Fact]
    public async Task NextOrderNumberAsync_ProducesIncreasingSequenceValues()
    {
        await using var context = Fixture.CreateContext();
        var repository = new OrderRepository(context, new SystemClock());

        var first = await repository.NextOrderNumberAsync(CancellationToken.None);
        var second = await repository.NextOrderNumberAsync(CancellationToken.None);

        first.Should().NotBe(second);
        first.Should().MatchRegex(@"^\d{8}-\d{4}$");
    }
}
