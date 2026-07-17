namespace PizzaShop.Application.Payments.Dtos;

/// <summary>Result of <c>InitializePaymentCommand</c> — where to redirect the customer to pay.</summary>
public sealed record InitializePaymentResultDto(string RedirectUrl);
