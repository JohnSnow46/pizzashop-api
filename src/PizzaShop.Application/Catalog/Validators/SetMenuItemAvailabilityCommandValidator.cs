using FluentValidation;
using PizzaShop.Application.Catalog.Commands;

namespace PizzaShop.Application.Catalog.Validators;

public sealed class SetMenuItemAvailabilityCommandValidator : AbstractValidator<SetMenuItemAvailabilityCommand>
{
    public SetMenuItemAvailabilityCommandValidator()
    {
        RuleFor(c => c.MenuItemId).NotEmpty();
    }
}
