using FluentValidation;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Orders.Validators;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Accepts PL phone numbers with an optional '+48' country code and optional space/dash
    /// separators between the three-digit groups (e.g. "123456789", "+48 123-456-789").
    /// </summary>
    private const string PhoneNumberPattern = @"^(\+48[\s-]?)?\d{3}([\s-]?\d{3}){2}$";

    public CreateOrderCommandValidator()
    {
        RuleFor(c => c.Contact).NotNull();
        When(c => c.Contact is not null, () =>
        {
            RuleFor(c => c.Contact.FullName).NotEmpty();
            RuleFor(c => c.Contact.PhoneNumber)
                .NotEmpty()
                .Matches(PhoneNumberPattern)
                .WithMessage("Phone number must be a valid PL phone number (e.g. '123456789' or '+48 123-456-789').");
            RuleFor(c => c.Contact.Email)
                .EmailAddress()
                .When(c => !string.IsNullOrEmpty(c.Contact.Email));
        });

        RuleFor(c => c.FulfillmentType).IsInEnum();
        RuleFor(c => c.PaymentMethod).IsInEnum();

        RuleFor(c => c.Items).NotEmpty();
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(1);
        });

        When(c => c.FulfillmentType == FulfillmentType.Delivery, () =>
        {
            RuleFor(c => c.DeliveryAddress)
                .NotNull()
                .WithMessage("A delivery address is required when the fulfillment type is delivery.");
        });

        RuleFor(c => c.PointsToRedeem)
            .GreaterThanOrEqualTo(0)
            .When(c => c.PointsToRedeem.HasValue);
    }
}
