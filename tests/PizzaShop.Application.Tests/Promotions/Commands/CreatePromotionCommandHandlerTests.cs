using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Promotions.Commands;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Tests.Promotions.Commands;

public class CreatePromotionCommandHandlerTests
{
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreatePromotionCommandHandler CreateHandler() =>
        new(_promotionRepository.Object, _unitOfWork.Object);

    private static CreatePromotionCommand ValidCommand() => new(
        "10% off",
        PromotionType.Percentage,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow.AddDays(30),
        10m,
        "SUMMER10",
        null,
        null);

    [Fact]
    public async Task Handle_ValidCommand_PersistsPromotionAndReturnsId()
    {
        Promotion? added = null;
        _promotionRepository
            .Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .Callback<Promotion, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Name.Should().Be("10% off");
        added.Code.Should().Be("SUMMER10");
        result.Should().Be(added.Id);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMinOrderValue_MapsMinOrderValueToMoney()
    {
        Promotion? added = null;
        _promotionRepository
            .Setup(r => r.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .Callback<Promotion, CancellationToken>((p, _) => added = p)
            .Returns(Task.CompletedTask);

        var command = ValidCommand() with { MinOrderValue = new MoneyDto(50m, "PLN") };

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        added!.MinOrderValue.Should().NotBeNull();
        added.MinOrderValue!.Amount.Should().Be(50m);
    }
}
