namespace PizzaShop.Application.Common.Dtos;

/// <summary>
/// DTO mirror of Domain's <see cref="PizzaShop.Domain.ValueObjects.Money"/>, shared across
/// modules — Queries never return Domain entities/VOs directly (application-layer.md 4).
/// </summary>
public sealed record MoneyDto(decimal Amount, string Currency);
