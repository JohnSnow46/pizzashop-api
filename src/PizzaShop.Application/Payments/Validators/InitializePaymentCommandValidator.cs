using FluentValidation;
using PizzaShop.Application.Payments.Commands;

namespace PizzaShop.Application.Payments.Validators;

public sealed class InitializePaymentCommandValidator : AbstractValidator<InitializePaymentCommand>
{
    public InitializePaymentCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
