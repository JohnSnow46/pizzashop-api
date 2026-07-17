using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed class UpdateDeliveryAreaCommandHandler : ICommandHandler<UpdateDeliveryAreaCommand>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeliveryAreaCommandHandler(IRestaurantRepository restaurantRepository, IUnitOfWork unitOfWork)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateDeliveryAreaCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        restaurant.UpdateDeliveryArea(new GeoCoordinate(command.Latitude, command.Longitude), command.DeliveryRadiusKm);

        await _restaurantRepository.UpdateAsync(restaurant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
