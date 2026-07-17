namespace PizzaShop.Application.Common.Exceptions;

/// <summary>
/// Thrown by <see cref="Abstractions.Payments.IPaymentGateway.VerifyAndParseNotification"/>
/// when a payment provider notification cannot be trusted — invalid/missing signature,
/// unrecognized sender, or a malformed payload. There is no authenticated caller/role behind
/// a provider webhook, so this is deliberately its own type: not a
/// <see cref="ForbiddenOperationException"/> (no notion of an executor's role) and not a
/// <see cref="ValidationException"/> (this isn't shape validation of a client DTO, and the
/// request never reaches <see cref="Behaviors.ValidationBehavior{TRequest}"/> as ordinary
/// input). Application-level, mapped to HTTP 400 in the Api middleware — 401 is also
/// acceptable if the implementation prefers to signal "unauthenticated" (ADR-0013,
/// application-layer.md 5).
/// </summary>
public sealed class InvalidPaymentNotificationException : Exception
{
    public InvalidPaymentNotificationException(string message) : base(message)
    {
    }
}
