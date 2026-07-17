using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Promotions.Queries;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Tests.Promotions.Queries;

public class GetPromotionsQueryHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotionRepository = new();

    private GetPromotionsQueryHandler CreateHandler() => new(_promotionRepository.Object);

    [Fact]
    public async Task Handle_ReturnsAllPromotionsAsDtos()
    {
        var promotion = Promotion.Create("10% off", PromotionType.Percentage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), 10m);
        _promotionRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetPromotionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(promotion.Id);
        result[0].Name.Should().Be(promotion.Name);
        result[0].IsActive.Should().BeTrue();
    }
}
