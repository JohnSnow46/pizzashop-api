using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Api.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IRestaurantRepository"/> — see <see cref="InMemoryUserAccountRepository"/>
/// for rationale. Single-tenant (ADR-0003/ADR-0015): seeds one default <see cref="DomainRestaurant"/>
/// record eagerly, since <see cref="IRestaurantRepository.GetAsync"/> is never expected to
/// return null. Open every day, the full day (<see cref="TimeOnly.MinValue"/>-<see cref="TimeOnly.MaxValue"/>) —
/// Orders tests (<c>tests/PizzaShop.Api.Tests/Orders</c>) place orders "now", so the schedule must not
/// depend on which day of the week (or minute) the test suite happens to run.
/// </summary>
public sealed class InMemoryRestaurantRepository : IRestaurantRepository
{
    private DomainRestaurant _restaurant = DomainRestaurant.Create(
        "PizzaShop Test Restaurant",
        new Address("Testowa", "1", "Kraków", "30-001"),
        new GeoCoordinate(50.0614, 19.9366),
        5,
        "Europe/Warsaw",
        BuildAlwaysOpenSchedule(),
        "123456789",
        new Money(10m));

    public Task<DomainRestaurant> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_restaurant);

    public Task UpdateAsync(DomainRestaurant restaurant, CancellationToken cancellationToken)
    {
        _restaurant = restaurant;
        return Task.CompletedTask;
    }

    private static OpeningHours BuildAlwaysOpenSchedule()
    {
        var schedule = new Dictionary<DayOfWeek, IReadOnlyList<TimeRange>>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            schedule[day] = new List<TimeRange> { new(TimeOnly.MinValue, TimeOnly.MaxValue) };

        return new OpeningHours(schedule);
    }
}
