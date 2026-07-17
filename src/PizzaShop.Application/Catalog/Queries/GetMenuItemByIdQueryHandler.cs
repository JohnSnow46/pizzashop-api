using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Catalog.Queries;

public sealed class GetMenuItemByIdQueryHandler : IQueryHandler<GetMenuItemByIdQuery, MenuItemDto>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuItemByIdQueryHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<MenuItemDto> Handle(GetMenuItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await _menuItemRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), query.Id);

        return MenuItemMapper.ToDto(item);
    }
}
