using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Domain.Orders;

/// <summary>
/// Order aggregate — the heart of the domain (domain-model.md 5). Owns two independent
/// state cycles, <see cref="OrderStatus"/> (fulfillment) and <see cref="PaymentStatus"/>
/// (payment), coupled only where ADR-0007 requires it.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = new();

    public Guid Id { get; }
    public string Number { get; }
    public Guid? CustomerId { get; }
    public ContactDetails Contact { get; }
    public FulfillmentType FulfillmentType { get; }
    public DeliveryAddress? DeliveryAddress { get; }
    public DateTimeOffset PlacedAt { get; }
    public DateTimeOffset? RequestedFulfillmentTime { get; }
    public DateTimeOffset? EstimatedReadyAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; }
    public PaymentStatus PaymentStatus { get; private set; }
    public Guid? AppliedPromotionId { get; private set; }
    public int PointsToEarn { get; private set; }
    public int PointsRedeemed { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money Subtotal { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public Money DeliveryFee { get; private set; } = null!;
    public Money Total { get; private set; } = null!;

    // EF Core materialization only (ADR-0020) — not used by Domain logic.
    private Order()
    {
    }

    private Order(
        Guid id,
        string number,
        Guid? customerId,
        ContactDetails contact,
        FulfillmentType fulfillmentType,
        DeliveryAddress? deliveryAddress,
        IEnumerable<OrderItem> items,
        DateTimeOffset placedAt,
        DateTimeOffset? requestedFulfillmentTime,
        PaymentMethod paymentMethod)
    {
        Id = id;
        Number = number;
        CustomerId = customerId;
        Contact = contact;
        FulfillmentType = fulfillmentType;
        DeliveryAddress = deliveryAddress;
        _items.AddRange(items);
        PlacedAt = placedAt;
        RequestedFulfillmentTime = requestedFulfillmentTime;
        PaymentMethod = paymentMethod;
    }

    /// <summary>
    /// Builds a new order, enforcing every aggregate invariant from domain-model.md 5.4.
    /// Takes the (single-tenant) <paramref name="restaurant"/> as a collaborator purely
    /// for validation/fee calculation — <c>Order</c> does not keep a reference to it
    /// (ADR-0003: there is only ever one restaurant).
    /// </summary>
    public static Order Create(
        string number,
        Guid? customerId,
        ContactDetails contact,
        FulfillmentType fulfillmentType,
        DeliveryAddress? deliveryAddress,
        IEnumerable<OrderItem> items,
        DateTimeOffset placedAt,
        DateTimeOffset? requestedFulfillmentTime,
        PaymentMethod paymentMethod,
        Restaurant restaurant)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Order number is required.", nameof(number));
        ArgumentNullException.ThrowIfNull(contact);
        ArgumentNullException.ThrowIfNull(restaurant);

        var itemList = items?.ToList() ?? new List<OrderItem>();
        if (itemList.Count == 0)
            throw new EmptyOrderException();

        if (fulfillmentType == FulfillmentType.Delivery && deliveryAddress is null)
            throw new DeliveryAddressRequiredException();

        if (fulfillmentType == FulfillmentType.Delivery && !restaurant.IsWithinDeliveryArea(deliveryAddress!.Coordinate))
        {
            var distanceKm = restaurant.Location.DistanceKmTo(deliveryAddress.Coordinate);
            throw new AddressOutsideDeliveryAreaException(distanceKm, restaurant.DeliveryRadiusKm);
        }

        ValidateFulfillmentTime(requestedFulfillmentTime, placedAt, restaurant);

        var subtotal = itemList.Aggregate(Money.Zero(), (sum, item) => sum.Add(item.LineTotal));

        if (restaurant.MinimumOrderValue is { } minimum && subtotal < minimum)
            throw new BelowMinimumOrderValueException(subtotal, minimum);

        var deliveryFee = CalculateDeliveryFee(fulfillmentType, subtotal, restaurant);

        var order = new Order(
            Guid.NewGuid(),
            number,
            customerId,
            contact,
            fulfillmentType,
            deliveryAddress,
            itemList,
            placedAt,
            requestedFulfillmentTime,
            paymentMethod)
        {
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            DiscountAmount = Money.Zero(),
            Status = OrderStatus.PendingAcceptance,
            PaymentStatus = PaymentStatus.Pending,
        };
        order.RecalculateTotal();

        return order;
    }

    private static void ValidateFulfillmentTime(DateTimeOffset? requestedTime, DateTimeOffset placedAt, Restaurant restaurant)
    {
        if (requestedTime is { } requested)
        {
            if (requested < placedAt)
                throw new PastFulfillmentTimeException(requested);

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(restaurant.TimeZoneId);
            if (!restaurant.OpeningHours.IsOpenAt(requested, timeZone))
                throw new RestaurantClosedException(requested);

            return;
        }

        if (!restaurant.CanAcceptOrderAt(placedAt))
            throw new RestaurantClosedException(placedAt);
    }

    private static Money CalculateDeliveryFee(FulfillmentType fulfillmentType, Money subtotal, Restaurant restaurant)
    {
        if (fulfillmentType == FulfillmentType.Pickup)
            return Money.Zero();

        if (restaurant.FreeDeliveryThreshold is { } threshold && subtotal >= threshold)
            return Money.Zero();

        return restaurant.DeliveryFee;
    }

    private void RecalculateTotal() => Total = Subtotal.Subtract(DiscountAmount).Add(DeliveryFee);

    // ---- Fulfillment status transitions (domain-model.md 5.3, 5.4 rule 7) ----

    public void Accept()
    {
        EnsureOrderTransitionAllowed(OrderStatus.PendingAcceptance, OrderStatus.Accepted);

        if (PaymentMethod == PaymentMethod.Online && PaymentStatus != PaymentStatus.Paid)
            throw new PaymentRequiredBeforeAcceptanceException();

        Status = OrderStatus.Accepted;
    }

    public void Reject()
    {
        EnsureOrderTransitionAllowed(OrderStatus.PendingAcceptance, OrderStatus.Rejected);
        Status = OrderStatus.Rejected;
    }

    public void StartPreparation()
    {
        EnsureOrderTransitionAllowed(OrderStatus.Accepted, OrderStatus.InPreparation);
        Status = OrderStatus.InPreparation;
    }

    public void MarkReady()
    {
        EnsureOrderTransitionAllowed(OrderStatus.InPreparation, OrderStatus.Ready);
        Status = OrderStatus.Ready;
    }

    public void StartDelivery()
    {
        if (FulfillmentType != FulfillmentType.Delivery)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.OutForDelivery);

        EnsureOrderTransitionAllowed(OrderStatus.Ready, OrderStatus.OutForDelivery);
        Status = OrderStatus.OutForDelivery;
    }

    public void Complete()
    {
        var expectedFrom = FulfillmentType == FulfillmentType.Delivery
            ? OrderStatus.OutForDelivery
            : OrderStatus.Ready;

        EnsureOrderTransitionAllowed(expectedFrom, OrderStatus.Completed);
        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Rejected or OrderStatus.Cancelled)
            throw new InvalidOrderStatusTransitionException(Status, OrderStatus.Cancelled);

        Status = OrderStatus.Cancelled;
    }

    private void EnsureOrderTransitionAllowed(OrderStatus expectedFrom, OrderStatus to)
    {
        if (Status != expectedFrom)
            throw new InvalidOrderStatusTransitionException(Status, to);
    }

    // ---- Payment status transitions (domain-model.md 5.3, ADR-0007) ----

    public void AuthorizePayment()
    {
        EnsurePaymentTransitionAllowed(PaymentStatus.Pending, PaymentStatus.Authorized);
        PaymentStatus = PaymentStatus.Authorized;
    }

    public void ConfirmPayment()
    {
        if (PaymentStatus is not (PaymentStatus.Pending or PaymentStatus.Authorized))
            throw new InvalidPaymentStatusTransitionException(PaymentStatus, PaymentStatus.Paid);

        PaymentStatus = PaymentStatus.Paid;
    }

    public void FailPayment()
    {
        if (PaymentStatus is not (PaymentStatus.Pending or PaymentStatus.Authorized))
            throw new InvalidPaymentStatusTransitionException(PaymentStatus, PaymentStatus.Failed);

        PaymentStatus = PaymentStatus.Failed;
    }

    public void RefundPayment()
    {
        EnsurePaymentTransitionAllowed(PaymentStatus.Paid, PaymentStatus.Refunded);
        PaymentStatus = PaymentStatus.Refunded;
    }

    private void EnsurePaymentTransitionAllowed(PaymentStatus expectedFrom, PaymentStatus to)
    {
        if (PaymentStatus != expectedFrom)
            throw new InvalidPaymentStatusTransitionException(PaymentStatus, to);
    }

    // ---- Scheduling (ADR-0008, domain-model.md 5.4 rule 9) ----

    public void SetEstimatedReadyAt(DateTimeOffset estimatedReadyAt)
    {
        if (Status is OrderStatus.PendingAcceptance or OrderStatus.Rejected or OrderStatus.Cancelled or OrderStatus.Completed)
            throw new InvalidEstimatedReadyAtException(
                $"Estimated ready time can only be set once the order has been accepted (current status: '{Status}').");

        if (estimatedReadyAt < PlacedAt)
            throw new InvalidEstimatedReadyAtException("Estimated ready time cannot be earlier than the time the order was placed.");

        EstimatedReadyAt = estimatedReadyAt;
    }

    // ---- Discounts and loyalty (domain-model.md 5.4 rule 10, 8, ADR-0009) ----

    public void ApplyPromotion(Guid promotionId, Money discountAmount)
    {
        if (AppliedPromotionId is not null)
            throw new PromotionAlreadyAppliedException();
        ArgumentNullException.ThrowIfNull(discountAmount);

        AppliedPromotionId = promotionId;
        DiscountAmount = DiscountAmount.Add(discountAmount);
        RecalculateTotal();
    }

    /// <summary>
    /// Registers points spent on this order. The available-balance check itself lives on
    /// <c>LoyaltyAccount</c> (which throws <see cref="InsufficientLoyaltyPointsException"/>);
    /// this method only enforces that redemption requires a registered customer.
    /// </summary>
    public void RedeemLoyaltyPoints(int points, Money discountAmount)
    {
        if (CustomerId is null)
            throw new LoyaltyRedemptionNotAllowedException();
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Redeemed points must be greater than zero.");
        if (PointsRedeemed > 0)
            throw new LoyaltyPointsAlreadyRedeemedException();
        ArgumentNullException.ThrowIfNull(discountAmount);

        PointsRedeemed = points;
        DiscountAmount = DiscountAmount.Add(discountAmount);
        RecalculateTotal();
    }

    /// <summary>
    /// Sets the loyalty points to award once the order reaches <see cref="OrderStatus.Completed"/>.
    /// The conversion rate itself lives in <c>ILoyaltyPolicy</c> (Application, ADR-0009);
    /// this is just the resulting value being recorded.
    /// </summary>
    public void SetPointsToEarn(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Points to earn cannot be negative.");

        PointsToEarn = points;
    }
}
