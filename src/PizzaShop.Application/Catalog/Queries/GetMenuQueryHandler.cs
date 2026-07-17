using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Queries;

public sealed class GetMenuQueryHandler : IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemDto>>
{
    private readonly IMenuItemRepository _menuItemRepository;

    public GetMenuQueryHandler(IMenuItemRepository menuItemRepository)
    {
        _menuItemRepository = menuItemRepository;
    }

    public async Task<IReadOnlyList<MenuItemDto>> Handle(GetMenuQuery query, CancellationToken cancellationToken)
    {
        var items = await _menuItemRepository.GetMenuAsync(cancellationToken);
        return items.Select(MenuItemMapper.ToDto).ToList();
    }
}
