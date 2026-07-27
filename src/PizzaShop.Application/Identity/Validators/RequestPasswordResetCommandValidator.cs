using FluentValidation;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Identity.Validators;

/// <summary>Shape validation only (api-layer.md 2.4) — email format.</summary>
public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
