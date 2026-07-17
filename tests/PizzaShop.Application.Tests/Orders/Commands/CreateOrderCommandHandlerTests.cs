using FluentAssertions;
using Moq;
using PizzaShop.Application.Abstractions.Geocoding;
using PizzaShop.Application.Abstractions.Loyalty;
using PizzaShop.Application.Abstractions.Payments;
using PizzaShop.Application.Abstractions.Persistence;
using PizzaShop.Application.Common.Abstractions;
using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Application.Orders.Commands;
using PizzaShop.Application.Orders.Dtos;
using PizzaShop.Application.Tests.TestHelpers;
using PizzaShop.Domain.Catalog;
using PizzaShop.Domain.Enums;
using PizzaShop.Domain.Exceptions;
using PizzaShop.Domain.Loyalty;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.Promotions;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Tests.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IRestaurantRepository> _restaurantRepository = new();
    private readonly Mock<IMenuItemRepository> _menuItemRepository = new();
    private readonly Mock<IIngredientRepository> _ingredientRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IPromotionRepository> _promotionRepository = new();
    private readonly Mock<ILoyaltyAccountRepository> _loyaltyAccountRepository = new();
    private readonly Mock<ILoyaltyPolicy> _loyaltyPolicy = new();
    private readonly Mock<IGeocodingService> _geocodingService = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IClock> _clock = new();

    public CreateOrderCommandHandlerTests()
    {
        _restaurantRepository
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderTestFactory.CreateOpenRestaurant());
        _orderRepository.Setup(r => r.NextOrderNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("ORD-0001");
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem>());
        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient>());
        _clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
    }

    private CreateOrderCommandHandler CreateHandler() =>
        new(
            _restaurantRepository.Object,
            _menuItemRepository.Object,
            _ingredientRepository.Object,
            _orderRepository.Object,
            _promotionRepository.Object,
            _loyaltyAccountRepository.Object,
            _loyaltyPolicy.Object,
            _geocodingService.Object,
            _paymentGateway.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _clock.Object);

    private static ContactDetailsDto SampleContact() => new("Jan Kowalski", "123456789");

    [Fact]
    public async Task Handle_GuestPickupOrder_PersistsOrderAndReturnsGuestTrackingToken()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        Order? added = null;
        Guid? capturedToken = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((order, token, _, _) =>
            {
                added = order;
                capturedToken = token;
            })
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 2, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.CustomerId.Should().BeNull();
        capturedToken.Should().NotBeNull();
        result.OrderId.Should().Be(added.Id);
        result.Number.Should().Be("ORD-0001");
        result.GuestTrackingToken.Should().Be(capturedToken);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LoggedInCustomer_DoesNotGenerateGuestTrackingToken()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.GuestTrackingToken.Should().BeNull();
        _orderRepository.Verify(
            r => r.AddAsync(It.Is<Order>(o => o.CustomerId == customerId), null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeliveryOrder_GeocodesAddressAndBuildsDeliveryAddress()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderTestFactory.NearbyPoint);

        Order? added = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((order, _, _, _) => added = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Delivery,
            new AddressDto("Client St", "2", "Warsaw", "00-002"),
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.DeliveryAddress.Should().NotBeNull();
        added.DeliveryAddress!.Coordinate.Should().Be(OrderTestFactory.NearbyPoint);
    }

    [Fact]
    public async Task Handle_DeliveryAddressCannotBeGeocoded_ThrowsNotFoundException()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });
        _geocodingService
            .Setup(g => g.GeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeoCoordinate?)null);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Delivery,
            new AddressDto("Client St", "2", "Warsaw", "00-002"),
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _orderRepository.Verify(
            r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownMenuItem_ThrowsNotFoundException()
    {
        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(Guid.NewGuid(), null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExtraNotAllowedForMenuItem_ThrowsExtraNotAllowedException()
    {
        var pizza = OrderTestFactory.CreatePizza();
        var extra = Ingredient.Create("Olives", new Money(3));

        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });
        _ingredientRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ingredient> { extra });

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, new[] { extra.Id }) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ExtraNotAllowedException>();
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsEmptyOrderException()
    {
        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            Array.Empty<CreateOrderItemDto>(),
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EmptyOrderException>();
    }

    [Fact]
    public async Task Handle_OnlinePayment_InitializesGatewayAndReturnsRedirectUrl()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });
        _paymentGateway
            .Setup(g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentInitResult("https://sandbox.payu.com/pay/123", "PAYU-123"));

        Order? added = null;
        string? capturedReference = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((order, _, reference, _) =>
            {
                added = order;
                capturedReference = reference;
            })
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.Online);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.PaymentRedirectUrl.Should().Be("https://sandbox.payu.com/pay/123");
        capturedReference.Should().Be("PAYU-123");
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(
                It.Is<PaymentInitRequest>(r => r.OrderId == added!.Id && r.Amount == added.Total),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OnPickupPayment_DoesNotCallGatewayAndReturnsNullRedirectUrl()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        string? capturedReference = "not-set";
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((_, _, reference, _) => capturedReference = reference)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.PaymentRedirectUrl.Should().BeNull();
        capturedReference.Should().BeNull();
        _paymentGateway.Verify(
            g => g.InitializePaymentAsync(It.IsAny<PaymentInitRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidPromotionCode_AppliesDiscountAndRecordsUsage()
    {
        var pizza = OrderTestFactory.CreatePizza(30m);
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var promotion = Promotion.Create(
            "10% off", PromotionType.Percentage, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), 10m, "SUMMER10");
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("SUMMER10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(promotion);

        Order? added = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((order, _, _, _) => added = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PromotionCode: "SUMMER10");

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.AppliedPromotionId.Should().Be(promotion.Id);
        added.DiscountAmount.Amount.Should().Be(3m);
        promotion.UsageCount.Should().Be(1);
        _promotionRepository.Verify(r => r.UpdateAsync(promotion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownPromotionCode_ThrowsNotFoundException()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });
        _promotionRepository
            .Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Promotion?)null);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PromotionCode: "MISSING");

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExpiredPromotionCode_ThrowsPromotionNotApplicableException()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var expiredPromotion = Promotion.Create(
            "Expired", PromotionType.Percentage, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1), 10m, "OLD10");
        _promotionRepository
            .Setup(r => r.GetByCodeAsync("OLD10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredPromotion);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PromotionCode: "OLD10");

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<PromotionNotApplicableException>();
    }

    [Fact]
    public async Task Handle_LoggedInCustomerRedeemsPoints_RedeemsFromLoyaltyAccountAndAppliesDiscount()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var pizza = OrderTestFactory.CreatePizza(30m);
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var loyaltyAccount = LoyaltyAccount.Create(customerId);
        loyaltyAccount.Earn(100, "Signup bonus", DateTimeOffset.UtcNow);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loyaltyAccount);
        _loyaltyPolicy.Setup(p => p.CalculateRedemptionValue(50)).Returns(new Money(5m));

        Order? added = null;
        _orderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Order, Guid?, string?, CancellationToken>((order, _, _, _) => added = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PointsToRedeem: 50);

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.PointsRedeemed.Should().Be(50);
        added.DiscountAmount.Amount.Should().Be(5m);
        loyaltyAccount.PointsBalance.Should().Be(50);
        _loyaltyAccountRepository.Verify(r => r.UpdateAsync(loyaltyAccount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GuestRequestsPointsRedemption_ThrowsLoyaltyRedemptionNotAllowedException()
    {
        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PointsToRedeem: 10);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<LoyaltyRedemptionNotAllowedException>();
    }

    [Fact]
    public async Task Handle_InsufficientLoyaltyBalance_ThrowsInsufficientLoyaltyPointsException()
    {
        var customerId = Guid.NewGuid();
        _currentUser.Setup(c => c.CustomerId).Returns(customerId);

        var pizza = OrderTestFactory.CreatePizza();
        _menuItemRepository
            .Setup(r => r.GetManyByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MenuItem> { pizza });

        var loyaltyAccount = LoyaltyAccount.Create(customerId);
        _loyaltyAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loyaltyAccount);
        _loyaltyPolicy.Setup(p => p.CalculateRedemptionValue(It.IsAny<int>())).Returns(new Money(5m));

        var command = new CreateOrderCommand(
            SampleContact(),
            FulfillmentType.Pickup,
            null,
            new[] { new CreateOrderItemDto(pizza.Id, null, 1, Array.Empty<Guid>()) },
            null,
            PaymentMethod.OnPickup,
            PointsToRedeem: 50);

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientLoyaltyPointsException>();
    }
}
