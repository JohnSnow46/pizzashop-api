using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Application.Abstractions.Persistence;

/// <summary>
/// Access to the single <see cref="DomainRestaurant"/> record — there is exactly one in a
/// single-tenant deployment, so it is fetched without an id (ADR-0015, ADR-0003).
/// </summary>
public interface IRestaurantRepository
{
    Task<DomainRestaurant> GetAsync(CancellationToken cancellationToken);

    Task UpdateAsync(DomainRestaurant restaurant, CancellationToken cancellationToken);
}
