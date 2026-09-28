using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Reviews.Dtos;

namespace PizzaShop.Application.Reviews.Queries;

public sealed class GetMyReviewsQueryHandler : IQueryHandler<GetMyReviewsQuery, IReadOnlyList<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyReviewsQueryHandler(IReviewRepository reviewRepository, ICurrentUser currentUser)
    {
        _reviewRepository = reviewRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ReviewDto>> Handle(GetMyReviewsQuery query, CancellationToken cancellationToken)
    {
        var customerId = _currentUser.CustomerId
            ?? throw new ForbiddenOperationException("Only registered customers have reviews.");

        var reviews = await _reviewRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return reviews.Select(ReviewMapper.ToDto).ToList();
    }
}
