using PizzaShop.Application.Common.Messaging;
using PizzaShop.Application.Restaurant.Dtos;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed record UpdateOpeningHoursCommand(OpeningHoursDto OpeningHours) : ICommand;
