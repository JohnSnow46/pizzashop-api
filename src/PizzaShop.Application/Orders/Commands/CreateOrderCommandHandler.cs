using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Orchestrates order creation (application-layer.md 4.3.1) — the full flow, steps 1-10:
/// promotion application (step 6, <see cref="ApplyPromotionAsync"/>) and loyalty point
/// redemption (step 7, <see cref="ApplyLoyaltyRedemptionAsync"/>) run before payment-gateway
/// initialization (step 8), since both can change <c>order.Total</c>.
/// </summary>
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, CreateOrderResultDto>
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILoyaltyAccountRepository _loyaltyAccountRepository;
    private readonly ILoyaltyPolicy _loyaltyPolicy;
    private readonly IGeocodingService _geocodingService;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public CreateOrderCommandHandler(
        IRestaurantRepository restaurantRepository,
        IMenuItemRepository menuItemRepository,
        IIngredientRepository ingredientRepository,
        IOrderRepository orderRepository,
        IPromotionRepository promotionRepository,
        ILoyaltyAccountRepository loyaltyAccountRepository,
        ILoyaltyPolicy loyaltyPolicy,
        IGeocodingService geocodingService,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _restaurantRepository = restaurantRepository;
        _menuItemRepository = menuItemRepository;
        _ingredientRepository = ingredientRepository;
        _orderRepository = orderRepository;
        _promotionRepository = promotionRepository;
        _loyaltyAccountRepository = loyaltyAccountRepository;
        _loyaltyPolicy = loyaltyPolicy;
        _geocodingService = geocodingService;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CreateOrderResultDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // Step 1: single-tenant restaurant record (ADR-0015).
        var restaurant = await _restaurantRepository.GetAsync(cancellationToken);

        // Step 2: geocode the delivery address, if any.
        var deliveryAddress = await ResolveDeliveryAddressAsync(command, cancellationToken);

        // Step 3: resolve cart lines into snapshot OrderItems.
        var items = await BuildOrderItemsAsync(command.Items, cancellationToken);

        // Step 4: human-readable order number.
        var number = await _orderRepository.NextOrderNumberAsync(cancellationToken);

        // Step 5: build the aggregate — Domain enforces every invariant (domain-model.md 5.4).
        var contact = new ContactDetails(command.Contact.FullName, command.Contact.PhoneNumber, command.Contact.Email);
        var order = Order.Create(
            number,
            _currentUser.CustomerId,
            contact,
            command.FulfillmentType,
            deliveryAddress,
            items,
            _clock.UtcNow,
            command.RequestedFulfillmentTime,
            command.PaymentMethod,
            restaurant);

        // Step 6: apply a promotion code, if supplied.
        await ApplyPromotionAsync(order, command.PromotionCode, cancellationToken);

        // Step 7: redeem loyalty points, if requested and the order belongs to a customer.
        await ApplyLoyaltyRedemptionAsync(order, command.PointsToRedeem, cancellationToken);

        // Step 8: for online payments, ask the gateway for a checkout redirect URL and a
        // reference to persist (ADR-0013/ADR-0018). OnPickup orders skip the gateway entirely
        // (ADR-0007).
        var paymentInit = await InitializeOnlinePaymentAsync(order, contact, cancellationToken);

        // Step 9: persist. A guest order (no CustomerId) gets an unguessable tracking token,
        // stored alongside the order but distinct from Order.Id (see IOrderRepository). The
        // gateway reference (null for OnPickup) is persisted in the same call (ADR-0018).
        var guestTrackingToken = _currentUser.CustomerId is null ? Guid.NewGuid() : (Guid?)null;
        await _orderRepository.AddAsync(order, guestTrackingToken, paymentInit?.ProviderPaymentReference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Step 10.
        return new CreateOrderResultDto(order.Id, order.Number, guestTrackingToken, paymentInit?.RedirectUrl);
    }

    private async Task<PaymentInitResult?> InitializeOnlinePaymentAsync(Order order, ContactDetails contact, CancellationToken cancellationToken)
    {
        if (order.PaymentMethod != PaymentMethod.Online)
            return null;

        return await _paymentGateway.InitializePaymentAsync(
            new PaymentInitRequest(order.Id, order.Number, order.Total, contact.Email, $"PizzaShop order {order.Number}"),
            cancellationToken);
    }

    /// <summary>
    /// Step 6 (application-layer.md 4.5): applies a coupon code, if supplied.
    /// <c>Promotion.CalculateDiscount</c> re-checks <c>IsQualifiedFor</c> itself and throws
    /// <see cref="PromotionNotApplicableException"/> if the cart no longer qualifies — Domain
    /// decides, this handler only orchestrates. The updated <c>Promotion</c> (usage count) is
    /// persisted in the same transaction as the order (step 9).
    /// </summary>
    private async Task ApplyPromotionAsync(Order order, string? promotionCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(promotionCode))
            return;

        var promotion = await _promotionRepository.GetByCodeAsync(promotionCode, cancellationToken)
            ?? throw new NotFoundException(nameof(Promotion), promotionCode);

        var lines = order.Items
            .Select(i => new OrderDiscountLine(i.MenuItemId, i.UnitPrice, i.Quantity))
            .ToList();
        var context = new OrderDiscountContext(order.Subtotal, order.DeliveryFee, _clock.UtcNow, promotionCode, lines);

        var discount = promotion.CalculateDiscount(context);
        order.ApplyPromotion(promotion.Id, discount);
        promotion.RecordUsage();

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
    }

    /// <summary>
    /// Step 7 (application-layer.md 4.6): redeems loyalty points, if requested. Only a
    /// registered customer's order carries a <c>CustomerId</c> (ADR-0005) — for a guest order,
    /// <c>Order.RedeemLoyaltyPoints</c> itself refuses with
    /// <see cref="LoyaltyRedemptionNotAllowedException"/> (Domain enforces this, not this
    /// handler). The available-balance check lives on <c>LoyaltyAccount.Redeem</c>.
    /// </summary>
    private async Task ApplyLoyaltyRedemptionAsync(Order order, int? pointsToRedeem, CancellationToken cancellationToken)
    {
        if (pointsToRedeem is not > 0)
            return;

        var points = pointsToRedeem.Value;

        if (order.CustomerId is not { } customerId)
        {
            order.RedeemLoyaltyPoints(points, Money.Zero());
            return;
        }

        var loyaltyAccount = await _loyaltyAccountRepository.GetByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LoyaltyAccount), customerId);

        var redemptionValue = _loyaltyPolicy.CalculateRedemptionValue(points);
        order.RedeemLoyaltyPoints(points, redemptionValue);
        loyaltyAccount.Redeem(points, $"Redeemed on order {order.Number}", _clock.UtcNow, order.Id);

        await _loyaltyAccountRepository.UpdateAsync(loyaltyAccount, cancellationToken);
    }

    private async Task<DeliveryAddress?> ResolveDeliveryAddressAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.FulfillmentType != FulfillmentType.Delivery)
            return null;

        var address = new Address(
            command.DeliveryAddress!.Street,
            command.DeliveryAddress.BuildingNumber,
            command.DeliveryAddress.City,
            command.DeliveryAddress.PostalCode,
            command.DeliveryAddress.ApartmentNumber,
            command.DeliveryAddress.Notes);

        var coordinate = await _geocodingService.GeocodeAsync(address, cancellationToken)
            ?? throw new NotFoundException("The delivery address could not be located.");

        return new DeliveryAddress(address, coordinate);
    }

    private async Task<List<OrderItem>> BuildOrderItemsAsync(
        IReadOnlyList<CreateOrderItemDto> itemInputs,
        CancellationToken cancellationToken)
    {
        var menuItemIds = itemInputs.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _menuItemRepository.GetManyByIdsAsync(menuItemIds, cancellationToken);
        var menuItemsById = menuItems.ToDictionary(m => m.Id);

        var extraIds = itemInputs.SelectMany(i => i.ExtraIngredientIds).Distinct().ToList();
        var ingredientsById = extraIds.Count == 0
            ? new Dictionary<Guid, Ingredient>()
            : (await _ingredientRepository.GetManyByIdsAsync(extraIds, cancellationToken)).ToDictionary(i => i.Id);

        var items = new List<OrderItem>();
        foreach (var input in itemInputs)
        {
            if (!menuItemsById.TryGetValue(input.MenuItemId, out var menuItem))
                throw new NotFoundException(nameof(MenuItem), input.MenuItemId);

            var (unitPrice, variantId, variantName) = menuItem.ResolvePrice(input.VariantId);

            var extras = new List<OrderItemExtra>();
            foreach (var extraId in input.ExtraIngredientIds)
            {
                if (!ingredientsById.TryGetValue(extraId, out var ingredient))
                    throw new NotFoundException(nameof(Ingredient), extraId);

                menuItem.EnsureExtraAllowed(ingredient);
                extras.Add(new OrderItemExtra(ingredient.Id, ingredient.Name, ingredient.ExtraPrice));
            }

            items.Add(OrderItem.Create(
                menuItem.Id,
                menuItem.Name,
                unitPrice,
                input.Quantity,
                variantId,
                variantName,
                extras,
                input.Notes));
        }

        return items;
    }
}
