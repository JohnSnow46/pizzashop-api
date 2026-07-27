using FluentValidation;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Identity.Validators;

/// <summary>Shape validation only (api-layer.md 2.4) — token presence, password strength.</summary>
public sealed class ConfirmPasswordResetCommandValidator : AbstractValidator<ConfirmPasswordResetCommand>
{
    public ConfirmPasswordResetCommandValidator()
    {
        RuleFor(c => c.Token).NotEmpty();

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
