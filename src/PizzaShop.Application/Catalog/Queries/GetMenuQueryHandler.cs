using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Queries;

public sealed class GetMenuQueryHandler : IQueryHandler<GetMenuQuery, IReadOnlyList<MenuItemDto>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IReviewRepository _reviewRepository;

    public GetMenuQueryHandler(IMenuItemRepository menuItemRepository, IReviewRepository reviewRepository)
    {
        _menuItemRepository = menuItemRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<IReadOnlyList<MenuItemDto>> Handle(GetMenuQuery query, CancellationToken cancellationToken)
    {
        var items = await _menuItemRepository.GetMenuAsync(cancellationToken);
        var ratingSummaries = await _reviewRepository.GetRatingSummariesAsync(items.Select(i => i.Id), cancellationToken);

        return items
            .Select(item => MenuItemMapper.ToDto(
                item,
                ratingSummaries.TryGetValue(item.Id, out var summary) ? summary : null))
            .ToList();
    }
}
