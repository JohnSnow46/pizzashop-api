using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Abstractions.Payments;

/// <summary>
/// A verified, parsed payment provider notification (ADR-0013). <see cref="Status"/> is
/// already mapped from the provider's raw status to the internal <see cref="PaymentStatus"/>
/// — PayU's vocabulary never leaks past <see cref="IPaymentGateway"/>.
/// </summary>
public sealed record PaymentNotification(Guid OrderId, string ProviderPaymentReference, PaymentStatus Status);
