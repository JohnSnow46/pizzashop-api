using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Payments.Dtos;

namespace PizzaShop.Application.Payments.Queries;

/// <summary>Reads the payment status of an order (application-layer.md 4.4).</summary>
public sealed record GetPaymentStatusQuery(Guid OrderId) : IQuery<PaymentStatusDto>;
