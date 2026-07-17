using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
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
}
