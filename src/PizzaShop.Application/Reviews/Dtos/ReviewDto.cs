namespace PizzaShop.Application.Reviews.Dtos;

public sealed record ReviewDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    Guid MenuItemId,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt);

/// <summary>Menu item's reviews plus the average rating, for the public catalog view (ADR-0042).</summary>
public sealed record MenuItemReviewsDto(
    Guid MenuItemId,
    double? AverageRating,
    IReadOnlyList<ReviewDto> Reviews);
