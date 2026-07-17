using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
