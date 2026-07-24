using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Promotions.Commands;

public sealed record DeactivatePromotionCommand(Guid PromotionId) : ICommand;
