using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Promotions.Queries;

public sealed class ValidatePromotionCodeQueryHandler : IQueryHandler<ValidatePromotionCodeQuery, PromotionDiscountPreviewDto>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IClock _clock;

    public ValidatePromotionCodeQueryHandler(IPromotionRepository promotionRepository, IClock clock)
    {
        _promotionRepository = promotionRepository;
        _clock = clock;
    }

    public async Task<PromotionDiscountPreviewDto> Handle(ValidatePromotionCodeQuery query, CancellationToken cancellationToken)
    {
        var promotion = await _promotionRepository.GetByCodeAsync(query.Code, cancellationToken);
        var subtotal = new Money(query.Subtotal.Amount, query.Subtotal.Currency);
        var now = _clock.UtcNow;

        if (promotion is null || !promotion.IsQualifiedFor(subtotal, now, query.Code))
            return new PromotionDiscountPreviewDto(false, null);

        var deliveryFee = new Money(query.DeliveryFee.Amount, query.DeliveryFee.Currency);
        var discount = promotion.CalculateDiscount(subtotal, deliveryFee, now, query.Code);

        return new PromotionDiscountPreviewDto(true, new MoneyDto(discount.Amount, discount.Currency));
    }
}
