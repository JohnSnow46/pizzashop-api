using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Promotions.Commands;

public sealed class DeactivatePromotionCommandHandler : ICommandHandler<DeactivatePromotionCommand>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePromotionCommandHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeactivatePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), command.PromotionId);

        promotion.Deactivate();

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
