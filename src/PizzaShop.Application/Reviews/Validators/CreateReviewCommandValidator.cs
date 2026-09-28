using FluentValidation;
using PizzaShop.Application.Reviews.Commands;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Reviews.Validators;

/// <summary>
/// Shape-only validation (ADR-0042 §D) — eligibility (ownership, order status, item
/// membership, duplicate) is a cross-aggregate concern checked in the handler, not here.
/// </summary>
public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(c => c.OrderId).NotEqual(Guid.Empty);
        RuleFor(c => c.MenuItemId).NotEqual(Guid.Empty);
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
        RuleFor(c => c.Comment).MaximumLength(Review.MaxCommentLength);
    }
}
