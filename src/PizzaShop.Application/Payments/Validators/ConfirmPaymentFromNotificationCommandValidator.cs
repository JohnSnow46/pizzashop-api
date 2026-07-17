using FluentValidation;
using PizzaShop.Application.Payments.Commands;

namespace PizzaShop.Application.Payments.Validators;

/// <summary>
/// Shape-only validation (CLAUDE.md) for the webhook payload: a non-empty raw body and
/// headers collection. Signature/authenticity is verified separately by
/// <c>IPaymentGateway.VerifyAndParseNotification</c>, not here.
/// </summary>
public sealed class ConfirmPaymentFromNotificationCommandValidator : AbstractValidator<ConfirmPaymentFromNotificationCommand>
{
    public ConfirmPaymentFromNotificationCommandValidator()
    {
        RuleFor(c => c.RawBody).NotEmpty();
        RuleFor(c => c.Headers).NotNull();
    }
}
