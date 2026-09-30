using FluentValidation;
using PizzaShop.Application.Carts.Commands;

namespace PizzaShop.Application.Carts.Validators;

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(c => c.MenuItemId).NotEqual(Guid.Empty);
        RuleFor(c => c.Quantity).InclusiveBetween(1, 100);
        RuleFor(c => c.Notes).MaximumLength(500);
    }
}
