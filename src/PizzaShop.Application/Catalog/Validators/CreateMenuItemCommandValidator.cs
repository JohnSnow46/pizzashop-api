using FluentValidation;
using PizzaShop.Application.Catalog.Commands;

namespace PizzaShop.Application.Catalog.Validators;

public sealed class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.BasePrice).NotNull();

        When(c => c.BasePrice is not null, () =>
        {
            RuleFor(c => c.BasePrice.Amount).GreaterThanOrEqualTo(0);
            RuleFor(c => c.BasePrice.Currency).NotEmpty();
        });

        RuleForEach(c => c.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.Name).NotEmpty();
            variant.RuleFor(v => v.Price).NotNull();
            variant.When(v => v.Price is not null, () =>
                variant.RuleFor(v => v.Price.Amount).GreaterThanOrEqualTo(0));
        });
    }
}
