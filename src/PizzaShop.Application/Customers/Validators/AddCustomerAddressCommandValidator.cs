using FluentValidation;
using PizzaShop.Application.Customers.Commands;

namespace PizzaShop.Application.Customers.Validators;

/// <summary>
/// Shape-only validation (ADR-0012). Length limits mirror the DB mapping for
/// <c>CustomerAddresses</c>/owned <c>Address</c> (infrastructure-layer.md 2.3, Shared/OwnedAddress).
/// </summary>
public sealed class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
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
