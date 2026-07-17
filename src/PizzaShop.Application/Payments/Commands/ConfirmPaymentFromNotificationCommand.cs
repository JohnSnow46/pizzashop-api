using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// Processes an asynchronous payment notification from PayU (application-layer.md 4.4,
/// ADR-0013). Called by the anonymous webhook endpoint in Api — there is no
/// <c>ICurrentUser</c> here on purpose, since PayU (not a logged-in user) calls this.
/// <see cref="RawBody"/>/<see cref="Headers"/> are passed through untouched so that signature
/// verification (which needs the exact bytes PayU sent) happens in
/// <c>IPaymentGateway.VerifyAndParseNotification</c> (Infrastructure), not in the controller
/// or here.
/// </summary>
public sealed record ConfirmPaymentFromNotificationCommand(
    string RawBody,
    IReadOnlyDictionary<string, string> Headers) : ICommand;
