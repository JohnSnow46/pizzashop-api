using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class MarkReadyCommandValidator : AbstractValidator<MarkReadyCommand>
{
    public MarkReadyCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
