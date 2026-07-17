using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application.Restaurant.Commands;

public sealed record UpdateDeliveryAreaCommand(double Latitude, double Longitude, double DeliveryRadiusKm) : ICommand;
