namespace PizzaShop.Application.Restaurant.Dtos;

public sealed record TimeRangeDto(TimeOnly Start, TimeOnly End);

/// <summary>
/// DTO mirror of Domain's <see cref="PizzaShop.Domain.ValueObjects.OpeningHours"/>: zero or
/// more ranges per day of week, absent/empty = closed all day.
/// </summary>
public sealed record OpeningHoursDto(IReadOnlyDictionary<DayOfWeek, IReadOnlyList<TimeRangeDto>> Schedule);
