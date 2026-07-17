using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class StartPreparationCommandValidator : AbstractValidator<StartPreparationCommand>
{
    public StartPreparationCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
