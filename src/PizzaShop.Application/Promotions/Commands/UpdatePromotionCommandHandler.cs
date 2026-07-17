using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Domain.Promotions;

namespace PizzaShop.Application.Promotions.Commands;

public sealed class UpdatePromotionCommandHandler : ICommandHandler<UpdatePromotionCommand>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePromotionCommandHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdatePromotionCommand command, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByIdAsync(command.PromotionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), command.PromotionId);

        if (command.IsActive)
            promotion.Activate();
        else
            promotion.Deactivate();

        if (command.ValidFrom is not null && command.ValidTo is not null)
            promotion.UpdateWindow(command.ValidFrom.Value, command.ValidTo.Value);

        if (command.Value is not null)
            promotion.UpdateValue(command.Value);

        if (command.UsageLimit is not null)
            promotion.UpdateUsageLimit(command.UsageLimit);

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
