using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Catalog.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Catalog;

namespace PizzaShop.Application.Catalog.Queries;

public sealed class GetMenuItemByIdQueryHandler : IQueryHandler<GetMenuItemByIdQuery, MenuItemDto>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IReviewRepository _reviewRepository;

    public GetMenuItemByIdQueryHandler(IMenuItemRepository menuItemRepository, IReviewRepository reviewRepository)
    {
        _menuItemRepository = menuItemRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<MenuItemDto> Handle(GetMenuItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await _menuItemRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MenuItem), query.Id);

        var ratingSummaries = await _reviewRepository.GetRatingSummariesAsync(new[] { item.Id }, cancellationToken);

        return MenuItemMapper.ToDto(item, ratingSummaries.TryGetValue(item.Id, out var summary) ? summary : null);
    }
}
