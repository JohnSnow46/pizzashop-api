using FluentAssertions;
using PizzaShop.Application.Reviews.Commands;
using PizzaShop.Application.Reviews.Validators;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Application.Tests.Reviews.Validators;

public class CreateReviewCommandValidatorTests
{
    private readonly CreateReviewCommandValidator _validator = new();

    private static CreateReviewCommand ValidCommand() => new(Guid.NewGuid(), Guid.NewGuid(), 5, "Great!");

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasErrorForOrderId()
    {
        var result = _validator.Validate(ValidCommand() with { OrderId = Guid.Empty });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateReviewCommand.OrderId));
    }

    [Fact]
    public void Validate_EmptyMenuItemId_HasErrorForMenuItemId()
    {
        var result = _validator.Validate(ValidCommand() with { MenuItemId = Guid.Empty });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateReviewCommand.MenuItemId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_HasErrorForRating(int rating)
    {
        var result = _validator.Validate(ValidCommand() with { Rating = rating });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateReviewCommand.Rating));
    }

    [Fact]
    public void Validate_CommentExceedingMaxLength_HasErrorForComment()
    {
        var tooLong = new string('a', Review.MaxCommentLength + 1);

        var result = _validator.Validate(ValidCommand() with { Comment = tooLong });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateReviewCommand.Comment));
    }

    [Fact]
    public void Validate_NullComment_HasNoErrors()
    {
        var result = _validator.Validate(ValidCommand() with { Comment = null });

        result.IsValid.Should().BeTrue();
    }
}
