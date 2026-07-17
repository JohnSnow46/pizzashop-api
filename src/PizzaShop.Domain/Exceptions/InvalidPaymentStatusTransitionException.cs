using PizzaShop.Domain.Enums;

namespace PizzaShop.Domain.Exceptions;

/// <summary>
/// Thrown when a payment status transition does not follow the allowed graph
/// (domain-model.md 5.3, ADR-0007). Not explicitly named in domain-model.md section 9,
/// added because the payment cycle needs the same kind of guard as OrderStatus.
/// </summary>
public sealed class InvalidPaymentStatusTransitionException : DomainException
{
    public InvalidPaymentStatusTransitionException(PaymentStatus from, PaymentStatus to)
        : base($"Cannot transition payment status from '{from}' to '{to}'.")
    {
    }
}
