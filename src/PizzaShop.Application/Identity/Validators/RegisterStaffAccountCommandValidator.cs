using FluentValidation;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Identity.Commands;

namespace PizzaShop.Application.Identity.Validators;

/// <summary>
/// Shape validation only (api-layer.md 2.4: "rola z enuma"). <see cref="UserRole.Customer"/>
/// is rejected here as an invalid *shape* for this specific command — it is not a role
/// <c>/api/auth/staff</c> can ever produce (use <see cref="RegisterCustomerCommand"/> instead).
/// The role-hierarchy rule ("who may create whom") is state-dependent
/// (<c>ICurrentUser.Role</c>) and stays in the handler as a <c>ForbiddenOperationException</c>
/// (ADR-0017) — not this validator's concern.
/// </summary>
public sealed class RegisterStaffAccountCommandValidator : AbstractValidator<RegisterStaffAccountCommand>
{
    public RegisterStaffAccountCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(c => c.Role)
            .IsInEnum()
            .NotEqual(UserRole.Customer)
            .WithMessage("Use /api/auth/register to create a Customer account.");
    }
}
