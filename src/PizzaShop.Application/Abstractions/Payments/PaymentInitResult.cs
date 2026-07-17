namespace PizzaShop.Application.Abstractions.Payments;

/// <summary>
/// Result of <see cref="IPaymentGateway.InitializePaymentAsync"/> (ADR-0013):
/// <see cref="RedirectUrl"/> is where the caller sends the customer to pay,
/// <see cref="ProviderPaymentReference"/> is the provider's own identifier for the payment
/// session (opaque to Domain/Application, kept only to correlate later notifications/refunds).
/// </summary>
public sealed record PaymentInitResult(string RedirectUrl, string ProviderPaymentReference);
