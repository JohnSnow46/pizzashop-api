using FluentValidation;
using PizzaShop.Application.Promotions.Commands;

namespace PizzaShop.Application.Promotions.Validators;

public sealed class DeactivatePromotionCommandValidator : AbstractValidator<DeactivatePromotionCommand>
{
    public DeactivatePromotionCommandValidator()
    {
        RuleFor(c => c.PromotionId).NotEmpty();
    }
}
