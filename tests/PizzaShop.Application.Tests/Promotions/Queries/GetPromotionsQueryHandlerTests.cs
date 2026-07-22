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

    [Fact]
    public async Task Handle_BuyXGetYPromotion_MapsBuyXGetYRuleToDto()
    {
        var triggerId = Guid.NewGuid();
        var rewardId = Guid.NewGuid();
        var rule = new BuyXGetYRule(triggerId, 2, rewardId, 1, 100m);
        var promotion = Promotion.Create(
            "2+1", PromotionType.BuyXGetY, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), null, null, null, null, rule);
        _promotionRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Promotion> { promotion });

        var handler = CreateHandler();

        var result = await handler.Handle(new GetPromotionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Value.Should().BeNull();
        result[0].BuyXGetY.Should().NotBeNull();
        result[0].BuyXGetY!.TriggerMenuItemId.Should().Be(triggerId);
        result[0].BuyXGetY!.RewardMenuItemId.Should().Be(rewardId);
    }
}
