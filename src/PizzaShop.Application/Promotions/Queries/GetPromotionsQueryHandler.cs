using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;

namespace PizzaShop.Application.Promotions.Queries;

public sealed class GetPromotionsQueryHandler : IQueryHandler<GetPromotionsQuery, IReadOnlyList<PromotionDto>>
{
    private readonly IPromotionRepository _promotionRepository;

    public GetPromotionsQueryHandler(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task<IReadOnlyList<PromotionDto>> Handle(GetPromotionsQuery query, CancellationToken cancellationToken)
    {
        var promotions = await _promotionRepository.GetAllAsync(cancellationToken);
        return promotions.Select(PromotionMapper.ToDto).ToList();
    }
}
