using PizzaShop.Domain.Enums;

namespace PizzaShop.Application.Payments.Dtos;

/// <summary>Result of <c>GetPaymentStatusQuery</c> (application-layer.md 4.4).</summary>
public sealed record PaymentStatusDto(Guid OrderId, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus);
