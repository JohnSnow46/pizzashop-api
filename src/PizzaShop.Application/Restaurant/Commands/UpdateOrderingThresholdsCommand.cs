using PizzaShop.Application.Common.Dtos;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed record UpdateOrderingThresholdsCommand(
    MoneyDto? MinimumOrderValue,
    MoneyDto? FreeDeliveryThreshold,
    MoneyDto DeliveryFee) : ICommand;
