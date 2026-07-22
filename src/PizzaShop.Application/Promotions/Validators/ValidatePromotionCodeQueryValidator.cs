using FluentValidation;
using PizzaShop.Application.Promotions.Queries;

namespace PizzaShop.Application.Promotions.Validators;

public sealed class ValidatePromotionCodeQueryValidator : AbstractValidator<ValidatePromotionCodeQuery>
{
    public ValidatePromotionCodeQueryValidator()
    {
        RuleFor(q => q.Code).NotEmpty();

        RuleFor(q => q.Subtotal).NotNull();
        When(q => q.Subtotal is not null, () => RuleFor(q => q.Subtotal.Amount).GreaterThanOrEqualTo(0));

        RuleFor(q => q.DeliveryFee).NotNull();
        When(q => q.DeliveryFee is not null, () => RuleFor(q => q.DeliveryFee.Amount).GreaterThanOrEqualTo(0));

        RuleForEach(q => q.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.MenuItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(1);
            line.RuleFor(l => l.UnitPrice).NotNull();
            line.When(l => l.UnitPrice is not null, () => line.RuleFor(l => l.UnitPrice.Amount).GreaterThanOrEqualTo(0));
        });
    }
}
