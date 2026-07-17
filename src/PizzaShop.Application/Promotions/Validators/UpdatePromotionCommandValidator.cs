using FluentValidation;
using PizzaShop.Application.Promotions.Commands;

namespace PizzaShop.Application.Promotions.Validators;

public sealed class UpdatePromotionCommandValidator : AbstractValidator<UpdatePromotionCommand>
{
    public UpdatePromotionCommandValidator()
    {
        RuleFor(c => c.PromotionId).NotEmpty();

        RuleFor(c => c)
            .Must(c => c.ValidFrom.HasValue == c.ValidTo.HasValue)
            .WithMessage("ValidFrom and ValidTo must be supplied together.")
            .WithName(nameof(UpdatePromotionCommand.ValidTo));

        When(c => c.ValidFrom.HasValue && c.ValidTo.HasValue, () =>
        {
            RuleFor(c => c.ValidTo)
                .GreaterThan(c => c.ValidFrom)
                .WithMessage("Valid-to must be after valid-from.");
        });

        // Type-dependent range validation (e.g. Percentage 0-100, FixedAmount > 0) lives in
        // Domain (Promotion.UpdateValue -> ValidateValue) — the validator only knows the
        // request shape, not the promotion's Type, so it just rejects a clearly-invalid shape
        // (negative value) and leaves the rest to Domain.
        RuleFor(c => c.Value)
            .GreaterThanOrEqualTo(0)
            .When(c => c.Value.HasValue)
            .WithMessage("Value must be non-negative when set.");

        RuleFor(c => c.UsageLimit)
            .GreaterThan(0)
            .When(c => c.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than zero when set.");
    }
}
