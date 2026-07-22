using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Promotions.Dtos;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Promotions;
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
        var lines = (query.Lines ?? Array.Empty<PromotionDiscountLineDto>())
            .Select(l => new OrderDiscountLine(l.MenuItemId, new Money(l.UnitPrice.Amount, l.UnitPrice.Currency), l.Quantity))
            .ToList();
        var context = new OrderDiscountContext(subtotal, deliveryFee, now, query.Code, lines);

        // BuyXGetY may still turn out not applicable (too few trigger/reward units) even though
        // the generic gates above passed — that is a preview outcome ("not qualified"), not an
        // error to propagate (domain-model.md 8.2, ADR-0034).
        try
        {
            var discount = promotion.CalculateDiscount(context);
            return new PromotionDiscountPreviewDto(true, new MoneyDto(discount.Amount, discount.Currency));
        }
        catch (PromotionNotApplicableException)
        {
            return new PromotionDiscountPreviewDto(false, null);
        }
    }
}
