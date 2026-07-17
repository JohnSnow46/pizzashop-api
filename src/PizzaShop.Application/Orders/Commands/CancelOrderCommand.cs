using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Orders.Commands;

/// <summary>
/// Cancels an order. Refunding an already-paid online order (application-layer.md 4.3.3,
/// via <c>IPaymentGateway.RefundAsync</c>, ADR-0018) is orchestrated by the handler alongside
/// the <c>OrderStatus</c> transition.
/// </summary>
public sealed record CancelOrderCommand(Guid OrderId) : ICommand;
