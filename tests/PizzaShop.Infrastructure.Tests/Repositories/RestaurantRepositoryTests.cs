using FluentAssertions;
using PizzaShop.Infrastructure.Persistence.Repositories;
using PizzaShop.Infrastructure.Tests.Fixtures;
using PizzaShop.Infrastructure.Tests.TestHelpers;

namespace PizzaShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Round-trip coverage for <see cref="RestaurantRepository"/> — in particular the
/// <c>OpeningHours</c> jsonb converter and the owned <c>Address</c>/<c>GeoCoordinate</c>
/// Value Objects (ADR-0020).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RestaurantRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public RestaurantRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAndGet_RoundTripsAddressGeoCoordinateAndOpeningHours()
    {
        var restaurant = DomainTestFactory.CreateRestaurant();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new RestaurantRepository(writeContext);
            await writeContext.Restaurants.AddAsync(restaurant);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new RestaurantRepository(readContext);

        var loaded = await readRepository.GetAsync(CancellationToken.None);

        loaded.Id.Should().Be(restaurant.Id);
        loaded.Address.Should().Be(restaurant.Address);
        loaded.Location.Should().Be(restaurant.Location);
        loaded.OpeningHours.Should().Be(restaurant.OpeningHours);
        loaded.DeliveryFee.Should().Be(restaurant.DeliveryFee);
        loaded.MinimumOrderValue.Should().Be(restaurant.MinimumOrderValue);
        loaded.FreeDeliveryThreshold.Should().Be(restaurant.FreeDeliveryThreshold);
    }
}
