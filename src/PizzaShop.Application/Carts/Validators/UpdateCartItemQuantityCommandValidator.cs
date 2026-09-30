using FluentValidation;
using PizzaShop.Application.Carts.Commands;

namespace PizzaShop.Application.Carts.Validators;

public sealed class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(c => c.CartItemId).NotEqual(Guid.Empty);
        RuleFor(c => c.Quantity).InclusiveBetween(1, 100);
    }
}
