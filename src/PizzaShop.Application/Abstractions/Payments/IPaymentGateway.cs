using PizzaShop.Application.Common.Exceptions;

namespace PizzaShop.Application.Abstractions.Payments;

/// <summary>
/// Provider-agnostic payment port (ADR-0002, ADR-0013). Implemented in Infrastructure against
/// PayU (Sandbox on start); Domain and the rest of Application never see PayU's vocabulary.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Starts an online payment session and returns a checkout redirect URL.</summary>
    Task<PaymentInitResult> InitializePaymentAsync(PaymentInitRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the signature of an incoming provider notification and maps its raw status
    /// to <see cref="PaymentNotification"/>. Synchronous: signature verification is pure
    /// computation (HMAC over the raw body), no I/O is involved.
    /// </summary>
    /// <exception cref="InvalidPaymentNotificationException">
    /// The signature is missing/invalid or the payload cannot be parsed — the notification
    /// cannot be trusted (ADR-0013, application-layer.md 5).
    /// </exception>
    PaymentNotification VerifyAndParseNotification(string rawBody, IReadOnlyDictionary<string, string> headers);

    /// <summary>Reverses a previously captured payment (ADR-0007 refund path).</summary>
    Task RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken);
}
