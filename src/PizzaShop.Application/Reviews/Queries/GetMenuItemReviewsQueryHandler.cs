using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reviews.Dtos;

namespace PizzaShop.Application.Reviews.Queries;

public sealed class GetMenuItemReviewsQueryHandler : IQueryHandler<GetMenuItemReviewsQuery, MenuItemReviewsDto>
{
    private readonly IReviewRepository _reviewRepository;

    public GetMenuItemReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<MenuItemReviewsDto> Handle(GetMenuItemReviewsQuery query, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepository.GetByMenuItemIdAsync(query.MenuItemId, cancellationToken);
        var average = reviews.Count == 0 ? (double?)null : reviews.Average(r => r.Rating);

        return new MenuItemReviewsDto(query.MenuItemId, average, reviews.Select(ReviewMapper.ToDto).ToList());
    }
}
