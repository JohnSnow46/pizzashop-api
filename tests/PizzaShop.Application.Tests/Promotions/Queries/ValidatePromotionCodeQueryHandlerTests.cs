using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Application.Promotions.Queries;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Tests.Promotions.Queries;

public class ValidatePromotionCodeQueryHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IClock> _clock = new();

    public ValidatePromotionCodeQueryHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    private ValidatePromotionCodeQueryHandler CreateHandler() => new(_promotionRepository.Object, _clock.Object);

    [Fact]
    public async Task Handle_QualifyingCode_ReturnsQualifiedPreviewWithDiscount()
    {
        var promotion = Promotion.Create(
            "10% off", PromotionType.Percentage, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), 10m, "SUMMER10");
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("SUMMER10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ValidatePromotionCodeQuery("SUMMER10", new MoneyDto(100m, "PLN"), new MoneyDto(10m, "PLN")),
            CancellationToken.None);

        result.IsQualified.Should().BeTrue();
        result.DiscountAmount.Should().NotBeNull();
        result.DiscountAmount!.Amount.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_UnknownCode_ReturnsNotQualified()
    {
        _promotionRepository
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ValidatePromotionCodeQuery("MISSING", new MoneyDto(100m, "PLN"), new MoneyDto(10m, "PLN")),
            CancellationToken.None);

        result.IsQualified.Should().BeFalse();
        result.DiscountAmount.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SubtotalBelowMinOrderValue_ReturnsNotQualified()
    {
        var promotion = Promotion.Create(
            "10% off",
            PromotionType.Percentage,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1),
            10m,
            "SUMMER10",
            new Domain.ValueObjects.Money(200m));
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("SUMMER10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ValidatePromotionCodeQuery("SUMMER10", new MoneyDto(50m, "PLN"), new MoneyDto(10m, "PLN")),
            CancellationToken.None);

        result.IsQualified.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_BuyXGetYWithEnoughTriggerUnits_ReturnsQualifiedPreviewWithDiscount()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new Domain.Promotions.BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = Domain.Promotions.Promotion.Create(
            "2+1", PromotionType.BuyXGetY, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), null, "PIZZA21", null, null, rule);
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("PIZZA21", It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ValidatePromotionCodeQuery(
                "PIZZA21",
                new MoneyDto(90m, "PLN"),
                new MoneyDto(10m, "PLN"),
                new[] { new PromotionDiscountLineDto(pizzaId, new MoneyDto(30m, "PLN"), 3) }),
            CancellationToken.None);

        result.IsQualified.Should().BeTrue();
        result.DiscountAmount!.Amount.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_BuyXGetYWithTooFewTriggerUnits_ReturnsNotQualified()
    {
        var pizzaId = Guid.NewGuid();
        var rule = new Domain.Promotions.BuyXGetYRule(pizzaId, 2, pizzaId, 1, 100m);
        var promotion = Domain.Promotions.Promotion.Create(
            "2+1", PromotionType.BuyXGetY, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), null, "PIZZA21", null, null, rule);
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("PIZZA21", It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        var handler = CreateHandler();

        var result = await handler.Handle(
            new ValidatePromotionCodeQuery(
                "PIZZA21",
                new MoneyDto(60m, "PLN"),
                new MoneyDto(10m, "PLN"),
                new[] { new PromotionDiscountLineDto(pizzaId, new MoneyDto(30m, "PLN"), 2) }),
            CancellationToken.None);

        result.IsQualified.Should().BeFalse();
        result.DiscountAmount.Should().BeNull();
    }
}
