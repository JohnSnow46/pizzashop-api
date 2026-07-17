using FluentValidation;
using PizzaShop.Application.Catalog.Commands;

namespace PizzaShop.Application.Catalog.Validators;

public sealed class CreateIngredientCommandValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.ExtraPrice).NotNull();

        When(c => c.ExtraPrice is not null, () =>
        {
            RuleFor(c => c.ExtraPrice.Amount).GreaterThanOrEqualTo(0);
            RuleFor(c => c.ExtraPrice.Currency).NotEmpty();
        });
    }
}
