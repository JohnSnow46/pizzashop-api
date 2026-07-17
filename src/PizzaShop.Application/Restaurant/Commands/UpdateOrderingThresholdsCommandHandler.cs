using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed class UpdateOrderingThresholdsCommandHandler : ICommandHandler<UpdateOrderingThresholdsCommand>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderingThresholdsCommandHandler(IRestaurantRepository restaurantRepository, IUnitOfWork unitOfWork)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateOrderingThresholdsCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        restaurant.UpdateOrderingThresholds(
            ToMoney(command.MinimumOrderValue),
            ToMoney(command.FreeDeliveryThreshold),
            new Money(command.DeliveryFee.Amount, command.DeliveryFee.Currency));

        await _restaurantRepository.UpdateAsync(restaurant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static Money? ToMoney(MoneyDto? dto) => dto is null ? null : new Money(dto.Amount, dto.Currency);
}
