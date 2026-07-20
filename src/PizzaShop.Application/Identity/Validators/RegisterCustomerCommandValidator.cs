using FluentValidation;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Identity.Validators;

/// <summary>Shape validation only (api-layer.md 2.4) — email format, password strength.</summary>
public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(c => c.FullName).NotEmpty().MaximumLength(200);

        RuleFor(c => c.PhoneNumber).MaximumLength(30);
    }
}
