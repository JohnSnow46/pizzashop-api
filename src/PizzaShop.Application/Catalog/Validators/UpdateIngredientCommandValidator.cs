using FluentValidation;
using PizzaShop.Application.Catalog.Commands;

namespace PizzaShop.Application.Catalog.Validators;

public sealed class UpdateIngredientCommandValidator : AbstractValidator<UpdateIngredientCommand>
{
    public UpdateIngredientCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.ExtraPrice).NotNull();

        When(c => c.ExtraPrice is not null, () =>
        {
            RuleFor(c => c.ExtraPrice.Amount).GreaterThanOrEqualTo(0);
            RuleFor(c => c.ExtraPrice.Currency).NotEmpty();
        });
    }
}
