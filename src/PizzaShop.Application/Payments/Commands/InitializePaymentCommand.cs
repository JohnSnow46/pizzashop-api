using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;

namespace PizzaShop.Application.Payments.Commands;

/// <summary>
/// (Re-)initializes an online payment session for an existing order (application-layer.md
/// 4.4). Normally this happens inline inside <c>CreateOrderCommand</c> for a fresh
/// <c>PaymentMethod.Online</c> order; this stand-alone command exists for retrying (e.g. the
/// customer abandoned the PayU checkout or the previous attempt failed) — it does not change
/// <c>Order.PaymentStatus</c> itself, it only asks the gateway for a new checkout URL.
/// </summary>
public sealed record InitializePaymentCommand(Guid OrderId) : ICommand<InitializePaymentResultDto>;
