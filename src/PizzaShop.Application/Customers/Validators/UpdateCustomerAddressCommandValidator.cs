using FluentValidation;
using PizzaShop.Application.Customers.Commands;

namespace PizzaShop.Application.Customers.Validators;

/// <summary>
/// Shape-only validation (ADR-0012), mirroring <see cref="AddCustomerAddressCommandValidator"/>
/// -- same field limits, minus <c>IsDefault</c> which this command doesn't change.
/// </summary>
public sealed class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    public UpdateCustomerAddressCommandValidator()
    {
        RuleFor(c => c.Label).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Address).NotNull();

        When(c => c.Address is not null, () =>
        {
            RuleFor(c => c.Address.Street).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Address.BuildingNumber).NotEmpty().MaximumLength(20);
            RuleFor(c => c.Address.ApartmentNumber).MaximumLength(20);
            RuleFor(c => c.Address.City).NotEmpty().MaximumLength(100);
            RuleFor(c => c.Address.PostalCode).NotEmpty().MaximumLength(10);
            RuleFor(c => c.Address.Notes).MaximumLength(500);
        });
    }
}
