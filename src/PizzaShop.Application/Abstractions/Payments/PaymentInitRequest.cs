using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Abstractions.Payments;

/// <summary>
/// Everything <see cref="IPaymentGateway.InitializePaymentAsync"/> needs to start an online
/// payment session with the provider (ADR-0013). Provider-agnostic — PayU-specific request
/// shaping happens inside the Infrastructure implementation.
/// </summary>
public sealed record PaymentInitRequest(
    Guid OrderId,
    string OrderNumber,
    Money Amount,
    string? CustomerEmail,
    string Description);
