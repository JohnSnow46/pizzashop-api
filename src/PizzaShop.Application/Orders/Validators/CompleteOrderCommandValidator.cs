using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
{
    public CompleteOrderCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
