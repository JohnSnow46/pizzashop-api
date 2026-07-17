using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Catalog.Commands;

public sealed record SetMenuItemAvailabilityCommand(Guid MenuItemId, bool IsAvailable) : ICommand;
