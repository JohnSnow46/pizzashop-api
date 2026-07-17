using FluentValidation;
using PizzaShop.Application.Orders.Commands;

namespace PizzaShop.Application.Orders.Validators;

public sealed class StartDeliveryCommandValidator : AbstractValidator<StartDeliveryCommand>
{
    public StartDeliveryCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty();
    }
}
