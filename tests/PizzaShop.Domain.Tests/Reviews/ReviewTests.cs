using FluentAssertions;
using PizzaShop.Domain.Reviews;

namespace PizzaShop.Domain.Tests.Reviews;

public class ReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Review CreateReview(int rating = 5, string? comment = "Great pizza!") =>
        Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, comment, Now);

    [Fact]
    public void Create_ValidArguments_SetsAllProperties()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();

        var review = Review.Create(orderId, customerId, menuItemId, 4, "Tasty", Now);

        review.OrderId.Should().Be(orderId);
        review.CustomerId.Should().Be(customerId);
        review.MenuItemId.Should().Be(menuItemId);
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Tasty");
        review.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_EmptyOrderId_ThrowsArgumentException()
    {
        var act = () => Review.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 5, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyCustomerId_ThrowsArgumentException()
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 5, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyMenuItemId_ThrowsArgumentException()
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 5, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_RatingOutOfRange_ThrowsArgumentOutOfRangeException(int rating)
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_RatingWithinRange_Succeeds(int rating)
    {
        var review = CreateReview(rating: rating);

        review.Rating.Should().Be(rating);
    }

    [Fact]
    public void Create_NullComment_Succeeds()
    {
        var review = CreateReview(comment: null);

        review.Comment.Should().BeNull();
    }

    [Fact]
    public void Create_WhitespaceOnlyComment_ThrowsArgumentException()
    {
        var act = () => CreateReview(comment: "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_CommentExceedingMaxLength_ThrowsArgumentException()
    {
        var tooLong = new string('a', Review.MaxCommentLength + 1);

        var act = () => CreateReview(comment: tooLong);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_CommentAtMaxLength_Succeeds()
    {
        var atLimit = new string('a', Review.MaxCommentLength);

        var review = CreateReview(comment: atLimit);

        review.Comment.Should().Be(atLimit);
    }

    [Fact]
    public void Create_CommentWithSurroundingWhitespace_IsTrimmed()
    {
        var review = CreateReview(comment: "  Great!  ");

        review.Comment.Should().Be("Great!");
    }
}
