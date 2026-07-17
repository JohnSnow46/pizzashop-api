using FluentValidation;
using PizzaShop.Application.Orders.Queries;

namespace PizzaShop.Application.Orders.Validators;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(q => q.OrderId).NotEmpty();
    }
}
