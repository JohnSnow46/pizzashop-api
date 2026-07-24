using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Tests.Promotions.Commands;

public class DeactivatePromotionCommandHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeactivatePromotionCommandHandler CreateHandler() =>
        new(_promotionRepository.Object, _unitOfWork.Object);

    private static Promotion CreateActivePromotion() =>
        Promotion.Create("10% off", PromotionType.Percentage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30), 10m);

    [Fact]
    public async Task Handle_ActivePromotion_DeactivatesAndSaves()
    {
        var promotion = CreateActivePromotion();
        _promotionRepository.Setup(r => r.GetByIdAsync(promotion.Id, It.IsAny<CancellationToken>())).ReturnsAsync(promotion);

        var handler = CreateHandler();

        await handler.Handle(new DeactivatePromotionCommand(promotion.Id), CancellationToken.None);

        promotion.IsActive.Should().BeFalse();
        _promotionRepository.Verify(r => r.UpdateAsync(promotion, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownPromotion_ThrowsNotFoundException()
    {
        _promotionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var handler = CreateHandler();

        var act = () => handler.Handle(new DeactivatePromotionCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
