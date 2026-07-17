using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class SetMenuItemAvailabilityCommandHandler : ICommandHandler<SetMenuItemAvailabilityCommand>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetMenuItemAvailabilityCommandHandler(IMenuItemRepository menuItemRepository, IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(SetMenuItemAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(command.MenuItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), command.MenuItemId);

        if (command.IsAvailable)
            menuItem.MarkAvailable();
        else
            menuItem.MarkUnavailable();

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
