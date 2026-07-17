using FluentValidation;
using PizzaShop.Application.Restaurant.Commands;

namespace PizzaShop.Application.Restaurant.Validators;

public sealed class UpdateOrderingThresholdsCommandValidator : AbstractValidator<UpdateOrderingThresholdsCommand>
{
    public UpdateOrderingThresholdsCommandValidator()
    {
        RuleFor(c => c.DeliveryFee).NotNull();
        When(c => c.DeliveryFee is not null, () =>
            RuleFor(c => c.DeliveryFee!.Amount).GreaterThanOrEqualTo(0));

        When(c => c.MinimumOrderValue is not null, () =>
            RuleFor(c => c.MinimumOrderValue!.Amount).GreaterThanOrEqualTo(0));

        When(c => c.FreeDeliveryThreshold is not null, () =>
            RuleFor(c => c.FreeDeliveryThreshold!.Amount).GreaterThanOrEqualTo(0));
    }
}
