using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class SetEstimatedReadyAtCommandValidator : AbstractValidator<SetEstimatedReadyAtCommand>
{
    public SetEstimatedReadyAtCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
