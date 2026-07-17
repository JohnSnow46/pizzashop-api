using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Abstractions.Payments;

/// <summary>
/// Everything <see cref="IPaymentGateway.RefundAsync"/> needs to reverse a previously
/// captured online payment (ADR-0007/ADR-0013).
/// </summary>
public sealed record PaymentRefundRequest(Guid OrderId, string ProviderPaymentReference, Money Amount);
