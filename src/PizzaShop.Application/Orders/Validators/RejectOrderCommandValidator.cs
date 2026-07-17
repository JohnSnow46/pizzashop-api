using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
{
    public RejectOrderCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
