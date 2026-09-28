using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Reviews.Dtos;

public static class ReviewMapper
{
    public static ReviewDto ToDto(Review review) =>
        new(review.Id, review.OrderId, review.CustomerId, review.MenuItemId, review.Rating, review.Comment, review.CreatedAt);
}
