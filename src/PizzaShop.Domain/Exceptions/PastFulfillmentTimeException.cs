namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a requested fulfillment time is in the past relative to when the order
/// is placed (domain-model.md 5.4 rule 6, ADR-0008). Kept separate from
/// <see cref="RestaurantClosedException"/> because the cause is different (timing vs.
/// opening hours) even though both stem from the same rule.
/// </summary>
public sealed class PastFulfillmentTimeException : DomainException
{
    public PastFulfillmentTimeException(DateTimeOffset requestedTime)
        : base($"Requested fulfillment time {requestedTime:O} is in the past.")
    {
    }
}
