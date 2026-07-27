using FluentValidation;
using PizzaShop.Application.Payments.Commands;

namespace PizzaShop.Application.Payments.Validators;

public sealed class InitializeGuestPaymentCommandValidator : AbstractValidator<InitializeGuestPaymentCommand>
{
    public InitializeGuestPaymentCommandValidator()
    {
        RuleFor(c => c.GuestTrackingToken).NotEmpty();
    }
}
