using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Promotions.Commands;

public sealed class CreatePromotionCommandHandler : ICommandHandler<CreatePromotionCommand, Guid>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePromotionCommandHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreatePromotionCommand command, CancellationToken cancellationToken)
    {
        var minOrderValue = command.MinOrderValue is null
            ? null
            : new Money(command.MinOrderValue.Amount, command.MinOrderValue.Currency);

        var promotion = Promotion.Create(
            command.Name,
            command.Type,
            command.ValidFrom,
            command.ValidTo,
            command.Value,
            command.Code,
            minOrderValue,
            command.UsageLimit);

        await _promotionRepository.AddAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return promotion.Id;
    }
}
