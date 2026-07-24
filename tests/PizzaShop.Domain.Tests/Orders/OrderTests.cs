using FluentAssertions;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly DateTimeOffset PlacedAt = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem SampleItem(decimal price = 25m, int quantity = 1) =>
        OrderItem.Create(Guid.NewGuid(), "Margherita", new Money(price), quantity);

    private static Order CreatePickupOrder(
        DomainRestaurant? restaurant = null,
        Guid? customerId = null,
        PaymentMethod paymentMethod = PaymentMethod.OnPickup,
        IEnumerable<OrderItem>? items = null,
        DateTimeOffset? requestedFulfillmentTime = null)
    {
        return Order.Create(
            "ORD-0001",
            customerId,
            OrderTestFixtures.SampleContact(),
            FulfillmentType.Pickup,
            deliveryAddress: null,
            items ?? new[] { SampleItem() },
            PlacedAt,
            requestedFulfillmentTime,
            paymentMethod,
            restaurant ?? OrderTestFixtures.CreateOpenAllWeekRestaurant());
    }

    private static Order CreateDeliveryOrder(
        DomainRestaurant? restaurant = null,
        DeliveryAddress? deliveryAddress = null,
        PaymentMethod paymentMethod = PaymentMethod.OnPickup,
        Guid? customerId = null)
    {
        return Order.Create(
            "ORD-0002",
            customerId,
            OrderTestFixtures.SampleContact(),
            FulfillmentType.Delivery,
            deliveryAddress ?? OrderTestFixtures.SampleDeliveryAddress(),
            new[] { SampleItem() },
            PlacedAt,
            requestedFulfillmentTime: null,
            paymentMethod,
            restaurant ?? OrderTestFixtures.CreateOpenAllWeekRestaurant());
    }

    // ---- Rule 1: minimum one item ----

    [Fact]
    public void Create_NoItems_ThrowsEmptyOrderException()
    {
        var act = () => CreatePickupOrder(items: Array.Empty<OrderItem>());

        act.Should().Throw<EmptyOrderException>();
    }

    // ---- Rule 2: delivery requires address ----

    [Fact]
    public void Create_DeliveryWithoutAddress_ThrowsDeliveryAddressRequiredException()
    {
        var act = () => Order.Create(
            "ORD-0003",
            null,
            OrderTestFixtures.SampleContact(),
            FulfillmentType.Delivery,
            deliveryAddress: null,
            new[] { SampleItem() },
            PlacedAt,
            null,
            PaymentMethod.OnPickup,
            OrderTestFixtures.CreateOpenAllWeekRestaurant());

        act.Should().Throw<DeliveryAddressRequiredException>();
    }

    [Fact]
    public void Create_PickupWithoutAddress_DoesNotThrow()
    {
        var act = () => CreatePickupOrder();

        act.Should().NotThrow();
    }

    // ---- Rule 3: address must be within delivery area ----

    [Fact]
    public void Create_DeliveryAddressOutsideRadius_ThrowsAddressOutsideDeliveryAreaException()
    {
        var farAddress = OrderTestFixtures.SampleDeliveryAddress(OrderTestFixtures.FarAwayPoint);

        var act = () => CreateDeliveryOrder(deliveryAddress: farAddress);

        act.Should().Throw<AddressOutsideDeliveryAreaException>();
    }

    [Fact]
    public void Create_DeliveryAddressWithinRadius_DoesNotThrow()
    {
        var act = () => CreateDeliveryOrder();

        act.Should().NotThrow();
    }

    // ---- Rule 4: minimum order value ----

    [Fact]
    public void Create_SubtotalBelowMinimumOrderValue_ThrowsBelowMinimumOrderValueException()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant(minimumOrderValue: new Money(100m));

        var act = () => CreatePickupOrder(restaurant, items: new[] { SampleItem(price: 25m) });

        act.Should().Throw<BelowMinimumOrderValueException>();
    }

    [Fact]
    public void Create_SubtotalAtOrAboveMinimumOrderValue_DoesNotThrow()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant(minimumOrderValue: new Money(20m));

        var act = () => CreatePickupOrder(restaurant, items: new[] { SampleItem(price: 25m) });

        act.Should().NotThrow();
    }

    // ---- Rule 5: free delivery threshold / delivery fee ----

    [Fact]
    public void Create_Pickup_DeliveryFeeIsZero()
    {
        var order = CreatePickupOrder();

        order.DeliveryFee.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_DeliverySubtotalBelowFreeThreshold_ChargesConfiguredDeliveryFee()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant(
            freeDeliveryThreshold: new Money(100m),
            deliveryFee: new Money(12m));

        var order = CreateDeliveryOrder(restaurant);

        order.DeliveryFee.Amount.Should().Be(12m);
    }

    [Fact]
    public void Create_DeliverySubtotalAtOrAboveFreeThreshold_DeliveryFeeIsZero()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant(
            freeDeliveryThreshold: new Money(20m),
            deliveryFee: new Money(12m));

        var order = CreateDeliveryOrder(restaurant);

        order.DeliveryFee.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_Total_EqualsSubtotalMinusDiscountPlusDeliveryFee()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant(deliveryFee: new Money(12m));

        var order = CreateDeliveryOrder(restaurant);

        order.Total.Amount.Should().Be(order.Subtotal.Amount - order.DiscountAmount.Amount + order.DeliveryFee.Amount);
    }

    // ---- Rule 6: opening hours / requested fulfillment time ----

    [Fact]
    public void Create_RequestedFulfillmentTimeInPast_ThrowsPastFulfillmentTimeException()
    {
        var pastTime = PlacedAt.AddHours(-1);

        var act = () => CreatePickupOrder(requestedFulfillmentTime: pastTime);

        act.Should().Throw<PastFulfillmentTimeException>();
    }

    [Fact]
    public void Create_RequestedFulfillmentTimeOutsideOpeningHours_ThrowsRestaurantClosedException()
    {
        var restaurant = OrderTestFixtures.CreateOpenAllWeekRestaurant();
        // No day is scheduled for hour 23:59:30+ / falls after all configured ranges end at 23:59.
        var closedTime = new DateTimeOffset(2024, 1, 1, 23, 59, 30, TimeSpan.Zero);

        var act = () => CreatePickupOrder(restaurant, requestedFulfillmentTime: closedTime);

        act.Should().Throw<RestaurantClosedException>();
    }

    [Fact]
    public void Create_AsapWhenRestaurantClosed_ThrowsRestaurantClosedException()
    {
        var closedRestaurant = OrderTestFixtures.CreateClosedRestaurant();

        var act = () => CreatePickupOrder(closedRestaurant);

        act.Should().Throw<RestaurantClosedException>();
    }

    [Fact]
    public void Create_AsapWhenRestaurantOpen_DoesNotThrow()
    {
        var act = () => CreatePickupOrder();

        act.Should().NotThrow();
    }

    // ---- Rule 7: order status transitions follow the graph ----

    [Fact]
    public void Accept_FromPendingAcceptanceOnPickupOrder_TransitionsToAccepted()
    {
        var order = CreatePickupOrder();

        order.Accept();

        order.Status.Should().Be(OrderStatus.Accepted);
    }

    [Fact]
    public void StartPreparation_BeforeAccepted_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = CreatePickupOrder();

        var act = order.StartPreparation;

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void FullPickupLifecycle_ReadyThenComplete_SkipsOutForDelivery()
    {
        var order = CreatePickupOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();

        order.Complete();

        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void StartDelivery_OnPickupOrder_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = CreatePickupOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();

        var act = order.StartDelivery;

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void FullDeliveryLifecycle_GoesThroughOutForDelivery()
    {
        var order = CreateDeliveryOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();

        order.StartDelivery();
        order.Status.Should().Be(OrderStatus.OutForDelivery);

        order.Complete();
        order.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public void Reject_FromPendingAcceptance_TransitionsToRejected()
    {
        var order = CreatePickupOrder();

        order.Reject();

        order.Status.Should().Be(OrderStatus.Rejected);
    }

    [Fact]
    public void Reject_AfterAccepted_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = CreatePickupOrder();
        order.Accept();

        var act = order.Reject;

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Cancel_BeforeCompleted_TransitionsToCancelled()
    {
        var order = CreatePickupOrder();
        order.Accept();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AfterCompleted_ThrowsInvalidOrderStatusTransitionException()
    {
        var order = CreatePickupOrder();
        order.Accept();
        order.StartPreparation();
        order.MarkReady();
        order.Complete();

        var act = order.Cancel;

        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    // ---- Rule 8: payment/fulfillment coupling (ADR-0007) ----

    [Fact]
    public void Accept_OnlinePaymentNotYetPaid_ThrowsPaymentRequiredBeforeAcceptanceException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);

        var act = order.Accept;

        act.Should().Throw<PaymentRequiredBeforeAcceptanceException>();
    }

    [Fact]
    public void Accept_OnlinePaymentConfirmedPaid_Succeeds()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();

        order.Accept();

        order.Status.Should().Be(OrderStatus.Accepted);
    }

    [Fact]
    public void Accept_OnPickupPaymentPending_SucceedsRegardlessOfPaymentStatus()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.OnPickup);

        order.Accept();

        order.Status.Should().Be(OrderStatus.Accepted);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    // ---- Payment status transitions (ADR-0007) ----

    [Fact]
    public void AuthorizePayment_FromPending_TransitionsToAuthorized()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);

        order.AuthorizePayment();

        order.PaymentStatus.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public void AuthorizePayment_AlreadyAuthorized_ThrowsInvalidPaymentStatusTransitionException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.AuthorizePayment();

        var act = order.AuthorizePayment;

        act.Should().Throw<InvalidPaymentStatusTransitionException>();
    }

    [Fact]
    public void AuthorizePayment_AfterPaid_ThrowsInvalidPaymentStatusTransitionException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();

        var act = order.AuthorizePayment;

        act.Should().Throw<InvalidPaymentStatusTransitionException>();
    }

    [Fact]
    public void FailPayment_FromPending_TransitionsToFailed()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);

        order.FailPayment();

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void FailPayment_FromAuthorized_TransitionsToFailed()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.AuthorizePayment();

        order.FailPayment();

        order.PaymentStatus.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void FailPayment_AfterPaid_ThrowsInvalidPaymentStatusTransitionException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();

        var act = order.FailPayment;

        act.Should().Throw<InvalidPaymentStatusTransitionException>();
    }

    [Fact]
    public void RefundPayment_FromPaid_TransitionsToRefunded()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();

        order.RefundPayment();

        order.PaymentStatus.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void RefundPayment_FromPending_ThrowsInvalidPaymentStatusTransitionException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);

        var act = order.RefundPayment;

        act.Should().Throw<InvalidPaymentStatusTransitionException>();
    }

    [Fact]
    public void RefundPayment_AlreadyRefunded_ThrowsInvalidPaymentStatusTransitionException()
    {
        var order = CreatePickupOrder(paymentMethod: PaymentMethod.Online);
        order.ConfirmPayment();
        order.RefundPayment();

        var act = order.RefundPayment;

        act.Should().Throw<InvalidPaymentStatusTransitionException>();
    }

    // ---- Rule 9: EstimatedReadyAt ----

    [Fact]
    public void SetEstimatedReadyAt_BeforeAccepted_ThrowsInvalidEstimatedReadyAtException()
    {
        var order = CreatePickupOrder();

        var act = () => order.SetEstimatedReadyAt(PlacedAt.AddMinutes(30));

        act.Should().Throw<InvalidEstimatedReadyAtException>();
    }

    [Fact]
    public void SetEstimatedReadyAt_AfterAccepted_Succeeds()
    {
        var order = CreatePickupOrder();
        order.Accept();

        order.SetEstimatedReadyAt(PlacedAt.AddMinutes(30));

        order.EstimatedReadyAt.Should().Be(PlacedAt.AddMinutes(30));
    }

    [Fact]
    public void SetEstimatedReadyAt_BeforePlacedAt_ThrowsInvalidEstimatedReadyAtException()
    {
        var order = CreatePickupOrder();
        order.Accept();

        var act = () => order.SetEstimatedReadyAt(PlacedAt.AddMinutes(-5));

        act.Should().Throw<InvalidEstimatedReadyAtException>();
    }

    // ---- Rule 10: loyalty points require a registered customer ----

    [Fact]
    public void RedeemLoyaltyPoints_GuestOrder_ThrowsLoyaltyRedemptionNotAllowedException()
    {
        var order = CreatePickupOrder(customerId: null);

        var act = () => order.RedeemLoyaltyPoints(100, new Money(5m));

        act.Should().Throw<LoyaltyRedemptionNotAllowedException>();
    }

    [Fact]
    public void RedeemLoyaltyPoints_RegisteredCustomer_AppliesDiscountAndReducesTotal()
    {
        var order = CreatePickupOrder(customerId: Guid.NewGuid());
        var totalBefore = order.Total;

        order.RedeemLoyaltyPoints(100, new Money(5m));

        order.PointsRedeemed.Should().Be(100);
        order.Total.Amount.Should().Be(totalBefore.Amount - 5m);
    }

    [Fact]
    public void RedeemLoyaltyPoints_AlreadyRedeemed_ThrowsLoyaltyPointsAlreadyRedeemedException()
    {
        var order = CreatePickupOrder(customerId: Guid.NewGuid());
        order.RedeemLoyaltyPoints(100, new Money(5m));

        var act = () => order.RedeemLoyaltyPoints(50, new Money(2m));

        act.Should().Throw<LoyaltyPointsAlreadyRedeemedException>();
    }

    [Fact]
    public void RedeemLoyaltyPoints_DiscountExceedsRemainingPayable_ThrowsLoyaltyRedemptionExceedsOrderValueException()
    {
        // Default pickup order: subtotal 25, no delivery fee -> remaining payable is 25.
        var order = CreatePickupOrder(customerId: Guid.NewGuid());

        var act = () => order.RedeemLoyaltyPoints(1000, new Money(30m));

        act.Should().Throw<LoyaltyRedemptionExceedsOrderValueException>();
        order.PointsRedeemed.Should().Be(0);
    }

    [Fact]
    public void RedeemLoyaltyPoints_DiscountEqualsRemainingPayable_Succeeds()
    {
        // Default pickup order: subtotal 25, no delivery fee -> remaining payable is 25.
        var order = CreatePickupOrder(customerId: Guid.NewGuid());

        order.RedeemLoyaltyPoints(500, new Money(25m));

        order.PointsRedeemed.Should().Be(500);
        order.Total.Amount.Should().Be(0m);
    }

    // ---- Promotions (domain-model.md 5.4 rule 10, ADR-0009) ----

    [Fact]
    public void ApplyPromotion_NotYetApplied_AppliesDiscountAndReducesTotal()
    {
        var order = CreatePickupOrder();
        var totalBefore = order.Total;

        order.ApplyPromotion(Guid.NewGuid(), new Money(3m));

        order.Total.Amount.Should().Be(totalBefore.Amount - 3m);
    }

    [Fact]
    public void ApplyPromotion_AlreadyApplied_ThrowsPromotionAlreadyAppliedException()
    {
        var order = CreatePickupOrder();
        order.ApplyPromotion(Guid.NewGuid(), new Money(3m));

        var act = () => order.ApplyPromotion(Guid.NewGuid(), new Money(2m));

        act.Should().Throw<PromotionAlreadyAppliedException>();
    }

    [Fact]
    public void SetPointsToEarn_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var order = CreatePickupOrder();

        var act = () => order.SetPointsToEarn(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
