using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Infrastructure.Payments.PayU;

/// <summary>
/// Maps PayU's raw order status to the Domain <see cref="PaymentStatus"/> (ADR-0022) — PayU's
/// vocabulary never leaks past <see cref="Application.Abstractions.Payments.IPaymentGateway"/>.
/// </summary>
public static class PayUStatusMapper
{
    public static PaymentStatus Map(string payUStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payUStatus);

        return payUStatus switch
        {
            "PENDING" => PaymentStatus.Pending,
            "WAITING_FOR_CONFIRMATION" => PaymentStatus.Authorized,
            "COMPLETED" => PaymentStatus.Paid,
            "CANCELED" => PaymentStatus.Failed,
            "REJECTED" => PaymentStatus.Failed,
            _ => throw new InvalidPaymentNotificationException($"Unrecognized PayU order status '{payUStatus}'."),
        };
    }
}
