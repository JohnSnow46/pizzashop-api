using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed class ToggleAcceptingOrdersCommandHandler : ICommandHandler<ToggleAcceptingOrdersCommand>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleAcceptingOrdersCommandHandler(IRestaurantRepository restaurantRepository, IUnitOfWork unitOfWork)
    {
        _restaurantRepository = restaurantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ToggleAcceptingOrdersCommand command, CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        if (command.IsAccepting)
            restaurant.StartAcceptingOrders();
        else
            restaurant.StopAcceptingOrders();

        await _restaurantRepository.UpdateAsync(restaurant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
