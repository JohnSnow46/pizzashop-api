using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Abstractions.Storage;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Catalog.Commands;

public sealed class UploadMenuItemImageCommandHandler : ICommandHandler<UploadMenuItemImageCommand, string>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuItemImageStorage _menuItemImageStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadMenuItemImageCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuItemImageStorage menuItemImageStorage,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository;
        _menuItemImageStorage = menuItemImageStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(UploadMenuItemImageCommand command, CancellationToken cancellationToken)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(command.MenuItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), command.MenuItemId);

        var imageUrl = await _menuItemImageStorage.SaveAsync(
            menuItem.Id, command.Content, command.FileExtension, cancellationToken);

        menuItem.UpdateDetails(menuItem.Description, imageUrl);

        await _menuItemRepository.UpdateAsync(menuItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}
