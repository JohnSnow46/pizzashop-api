namespace PizzaShop.Application.Common.Dtos;

/// <summary>
/// DTO mirror of Domain's <see cref="PizzaShop.Domain.ValueObjects.GeoCoordinate"/>.
/// </summary>
public sealed record GeoCoordinateDto(double Latitude, double Longitude);
