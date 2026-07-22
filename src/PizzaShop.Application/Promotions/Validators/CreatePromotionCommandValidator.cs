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

        When(c => c.Type == PromotionType.BuyXGetY, () =>
        {
            RuleFor(c => c.Value)
                .Null()
                .WithMessage("BuyXGetY promotions must not set Value; configuration lives in BuyXGetY.");

            RuleFor(c => c.BuyXGetY)
                .NotNull()
                .WithMessage("BuyXGetY promotions require BuyXGetY configuration.");

            When(c => c.BuyXGetY is not null, () =>
            {
                RuleFor(c => c.BuyXGetY!.TriggerMenuItemId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("Trigger menu item id is required.");
                RuleFor(c => c.BuyXGetY!.BuyQuantity)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("Buy quantity must be at least 1.");
                RuleFor(c => c.BuyXGetY!.RewardMenuItemId)
                    .NotEqual(Guid.Empty)
                    .WithMessage("Reward menu item id is required.");
                RuleFor(c => c.BuyXGetY!.GetQuantity)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("Get quantity must be at least 1.");
                RuleFor(c => c.BuyXGetY!.RewardDiscountPercentage)
                    .Must(v => v is > 0 and <= 100)
                    .WithMessage("Reward discount percentage must be greater than 0 and at most 100.");
            });
        }).Otherwise(() =>
        {
            RuleFor(c => c.BuyXGetY)
                .Null()
                .WithMessage("BuyXGetY configuration is only allowed for BuyXGetY promotions.");
        });
    }
}
