using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IRestaurantRepository"/> — see <see cref="InMemoryUserAccountRepository"/>
/// for rationale. Single-tenant (ADR-0003/ADR-0015): seeds one default <see cref="DomainRestaurant"/>
/// record eagerly, since <see cref="IRestaurantRepository.GetAsync"/> is never expected to
/// return null.
/// </summary>
public sealed class InMemoryRestaurantRepository : IRestaurantRepository
{
    private DomainRestaurant _restaurant = DomainRestaurant.Create(
        "PizzaShop Test Restaurant",
        new Address("Testowa", "1", "Kraków", "30-001"),
        new GeoCoordinate(50.0614, 19.9366),
        5,
        "Europe/Warsaw",
        new OpeningHours(new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>
        {
            [DayOfWeek.Monday] = new List<TimeRange> { new(new TimeOnly(10, 0), new TimeOnly(22, 0)) },
        }),
        "123456789",
        new Money(10m));

    public Task<DomainRestaurant> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_restaurant);

    public Task UpdateAsync(DomainRestaurant restaurant, CancellationToken cancellationToken)
    {
        _restaurant = restaurant;
        return Task.CompletedTask;
    }
}
