using Microsoft.EntityFrameworkCore;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Exceptions;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRestaurantRepository"/> — there is exactly one
/// <see cref="DomainRestaurant"/> row in this single-tenant deployment (ADR-0003/ADR-0015).
/// </summary>
public sealed class RestaurantRepository : IRestaurantRepository
{
    private readonly PizzaShopDbContext _context;

    public RestaurantRepository(PizzaShopDbContext context)
    {
        _context = context;
    }

    public async Task<DomainRestaurant> GetAsync(CancellationToken cancellationToken)
    {
        var restaurant = await _context.Restaurants.SingleOrDefaultAsync(cancellationToken);

        return restaurant ?? throw new NotFoundException(nameof(DomainRestaurant), "singleton");
    }

    public Task UpdateAsync(DomainRestaurant restaurant, CancellationToken cancellationToken)
    {
        _context.Restaurants.Update(restaurant);
        return Task.CompletedTask;
    }
}
