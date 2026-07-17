using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed class UpdateOpeningHoursCommandHandler : ICommandHandler<UpdateOpeningHoursCommand>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOpeningHoursCommandHandler(IRestaurantRepository restaurantRepository, IUnitOfWork unitOfWork)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateOpeningHoursCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        restaurant.UpdateOpeningHours(RestaurantMapper.ToDomain(command.OpeningHours));

        await _restaurantRepository.UpdateAsync(restaurant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
