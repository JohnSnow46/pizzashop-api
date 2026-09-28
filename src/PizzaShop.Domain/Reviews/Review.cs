namespace PizzaShop.Domain.Reviews;

/// <summary>
/// A customer's rating (and optional comment) for one menu item from one of their orders
/// (domain-model.md, ADR-0042). Deliberately holds only <see cref="Guid"/> references to
/// <c>Order</c>/<c>MenuItem</c>, never object references — same cross-aggregate pattern as
/// <c>Promotion</c>/<c>OrderDiscountContext</c> (ADR-0011). Eligibility to create a review
/// (ownership, order status, item membership, one-per-pair) is a cross-aggregate concern and
/// is enforced by the Application handler, not here (ADR-0042) — this aggregate only protects
/// its own shape.
/// </summary>
public sealed class Review
{
    public const int MaxCommentLength = 1000;

    public Guid Id { get; }
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public Guid MenuItemId { get; }
    public int Rating { get; }
    public string? Comment { get; }
    public DateTimeOffset CreatedAt { get; }

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Review()
    {
    }

    private Review(Guid id, Guid orderId, Guid customerId, Guid menuItemId, int rating, string? comment, DateTimeOffset createdAt)
    {
        Id = id;
        OrderId = orderId;
        CustomerId = customerId;
        MenuItemId = menuItemId;
        Rating = rating;
        Comment = comment;
        CreatedAt = createdAt;
    }

    public static Review Create(Guid orderId, Guid customerId, Guid menuItemId, int rating, string? comment, DateTimeOffset createdAt)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id is required.", nameof(orderId));
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (menuItemId == Guid.Empty)
            throw new ArgumentException("Menu item id is required.", nameof(menuItemId));
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        var trimmedComment = comment?.Trim();
        if (trimmedComment is not null)
        {
            if (trimmedComment.Length == 0)
                throw new ArgumentException("Comment must not be empty when provided.", nameof(comment));
            if (trimmedComment.Length > MaxCommentLength)
                throw new ArgumentException($"Comment must not exceed {MaxCommentLength} characters.", nameof(comment));
        }

        return new Review(Guid.NewGuid(), orderId, customerId, menuItemId, rating, trimmedComment, createdAt);
    }
}
