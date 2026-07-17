using FluentValidation;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Promotions.Validators;

public sealed class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
{
    public CreatePromotionCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();

        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Type)
            .NotEqual(PromotionType.BuyXGetY)
            .WithMessage("BuyXGetY promotions are not supported yet (ADR-0011).");

        RuleFor(c => c.ValidTo)
            .GreaterThan(c => c.ValidFrom)
            .WithMessage("Valid-to must be after valid-from.");

        When(c => c.Type == PromotionType.Percentage, () =>
        {
            RuleFor(c => c.Value).NotNull().WithMessage("Value is required for Percentage promotions.");
            RuleFor(c => c.Value)
                .Must(v => v is > 0 and <= 100)
                .When(c => c.Value.HasValue)
                .WithMessage("Percentage value must be greater than 0 and at most 100.");
        });

        When(c => c.Type == PromotionType.FixedAmount, () =>
        {
            RuleFor(c => c.Value).NotNull().WithMessage("Value is required for FixedAmount promotions.");
            RuleFor(c => c.Value)
                .Must(v => v is > 0)
                .When(c => c.Value.HasValue)
                .WithMessage("Fixed amount value must be greater than zero.");
        });

        RuleFor(c => c.UsageLimit)
            .GreaterThan(0)
            .When(c => c.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than zero when set.");

        When(c => c.MinOrderValue is not null, () =>
        {
            RuleFor(c => c.MinOrderValue!.Amount).GreaterThanOrEqualTo(0);
            RuleFor(c => c.MinOrderValue!.Currency).NotEmpty();
        });
    }
}
