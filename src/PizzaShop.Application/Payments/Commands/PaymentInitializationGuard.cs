using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Orders;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Guards a case Domain itself has no method for (initializing a gateway session isn't an
/// <c>Order</c> state transition) and is universal rather than role-dependent — a state
/// conflict illegal for every caller, signalled with <see cref="ConflictException"/> (409,
/// ADR-0018), not <see cref="ForbiddenOperationException"/> (which is reserved for
/// role-dependent denials, ADR-0017). Shared by <see cref="InitializePaymentCommandHandler"/>
/// (owner/staff retry) and <see cref="InitializeGuestPaymentCommandHandler"/> (guest retry via
/// tracking token, ADR-0041).
/// </summary>
internal static class PaymentInitializationGuard
{
    public static void EnsureCanInitializePayment(Order order)
    {
        if (order.PaymentMethod != PaymentMethod.Online)
            throw new ConflictException("Payment can only be initialized for orders placed with online payment.");

        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new ConflictException("This order has already been paid.");
    }
}
