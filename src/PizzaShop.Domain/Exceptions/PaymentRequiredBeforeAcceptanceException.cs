namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when an order paid <c>Online</c> is accepted before its payment is confirmed
/// as <c>Paid</c> (domain-model.md 5.4 rule 8, ADR-0007).
/// </summary>
public sealed class PaymentRequiredBeforeAcceptanceException : DomainException
{
    public PaymentRequiredBeforeAcceptanceException()
        : base("Online payment must be confirmed as paid before the order can be accepted.")
    {
    }
}
