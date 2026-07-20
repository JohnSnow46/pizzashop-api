using FluentValidation;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Identity.Validators;

/// <summary>Shape validation only (api-layer.md 2.4) — credentials-matching is a handler concern.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();

        // No MinimumLength here on purpose: login must keep accepting shorter passwords set
        // before any minimum-length policy existed. MaximumLength is only an upper bound to
        // protect BCrypt.Verify from being run against pathologically long input.
        RuleFor(c => c.Password).NotEmpty().MaximumLength(100);
    }
}
