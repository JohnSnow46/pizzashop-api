using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Tests.Promotions.Commands;

public class UpdatePromotionCommandHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdatePromotionCommandHandler CreateHandler() =>
        new(_promotionRepository.Object, _unitOfWork.Object);

    private static Promotion CreateActivePromotion() =>
        Promotion.Create("10% off", PromotionType.Percentage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), 10m);

    [Fact]
    public async Task Handle_DeactivateActivePromotion_SetsIsActiveFalse()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: false), CancellationToken.None);

        promotion.IsActive.Should().BeFalse();
        _promotionRepository.Verify(r => r.UpdateAsync(promotion, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReactivateInactivePromotion_SetsIsActiveTrue()
    {
        var promotion = CreateActivePromotion();
        promotion.Deactivate();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true), CancellationToken.None);

        promotion.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnknownPromotion_ThrowsNotFoundException()
    {
        _promotionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new UpdatePromotionCommand(Guid.NewGuid(), IsActive: false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidFromAndValidToSupplied_UpdatesWindow()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);
        var newFrom = DateTimeOffset.UtcNow.AddDays(5);
        var newTo = DateTimeOffset.UtcNow.AddDays(60);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true, ValidFrom: newFrom, ValidTo: newTo), CancellationToken.None);

        promotion.ValidFrom.Should().Be(newFrom);
        promotion.ValidTo.Should().Be(newTo);
        _promotionRepository.Verify(r => r.UpdateAsync(promotion, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidFromAndValidToAbsent_LeavesWindowUnchanged()
    {
        var promotion = CreateActivePromotion();
        var originalFrom = promotion.ValidFrom;
        var originalTo = promotion.ValidTo;
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true), CancellationToken.None);

        promotion.ValidFrom.Should().Be(originalFrom);
        promotion.ValidTo.Should().Be(originalTo);
    }

    [Fact]
    public async Task Handle_ValueSupplied_UpdatesValue()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true, Value: 25m), CancellationToken.None);

        promotion.Value.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_ValueAbsent_LeavesValueUnchanged()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true), CancellationToken.None);

        promotion.Value.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_UsageLimitSupplied_UpdatesUsageLimit()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true, UsageLimit: 5), CancellationToken.None);

        promotion.UsageLimit.Should().Be(5);
    }

    [Fact]
    public async Task Handle_UsageLimitAbsent_LeavesUsageLimitUnchanged()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true), CancellationToken.None);

        promotion.UsageLimit.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WindowValueAndUsageLimitAllSupplied_UpdatesAllThree()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);
        var newFrom = DateTimeOffset.UtcNow.AddDays(1);
        var newTo = DateTimeOffset.UtcNow.AddDays(10);

        var handler = CreateHandler();

        await handler.Handle(
            new UpdatePromotionCommand(promotion.Id, IsActive: true, ValidFrom: newFrom, ValidTo: newTo, Value: 30m, UsageLimit: 2),
            CancellationToken.None);

        promotion.ValidFrom.Should().Be(newFrom);
        promotion.ValidTo.Should().Be(newTo);
        promotion.Value.Should().Be(30m);
        promotion.UsageLimit.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UsageLimitBelowCurrentUsageCount_UpdatesUsageLimitWithoutThrowing()
    {
        var promotion = CreateActivePromotion();
        promotion.RecordUsage();
        promotion.RecordUsage();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new UpdatePromotionCommand(promotion.Id, IsActive: true, UsageLimit: 1), CancellationToken.None);

        promotion.UsageLimit.Should().Be(1);
        promotion.UsageCount.Should().Be(2);
    }
}
