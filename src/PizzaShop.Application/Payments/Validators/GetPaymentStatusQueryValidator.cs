using FluentValidation;
using PizzaShop.Application.Payments.Queries;

namespace PizzaShop.Application.Payments.Validators;

public sealed class GetPaymentStatusQueryValidator : AbstractValidator<GetPaymentStatusQuery>
{
    public GetPaymentStatusQueryValidator()
    {
        RuleFor(q => q.OrderId).NotEmpty();
    }
}
