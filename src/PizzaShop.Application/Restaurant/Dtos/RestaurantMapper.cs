using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.ValueObjects;
using DomainRestaurant = PizzaShop.Domain.Restaurant;

namespace PizzaShop.Application.Restaurant.Dtos;

internal static class RestaurantMapper
{
    public static RestaurantConfigDto ToDto(DomainRestaurant restaurant) =>
        new(
            restaurant.Id,
            restaurant.Name,
            ToDto(restaurant.Address),
            new GeoCoordinateDto(restaurant.Location.Latitude, restaurant.Location.Longitude),
            restaurant.DeliveryRadiusKm,
            restaurant.TimeZoneId,
            ToDto(restaurant.OpeningHours),
            restaurant.ContactPhone,
            restaurant.IsAcceptingOrders,
            ToDto(restaurant.MinimumOrderValue),
            ToDto(restaurant.FreeDeliveryThreshold),
            ToDto(restaurant.DeliveryFee)!);

    public static RestaurantInfoDto ToInfoDto(DomainRestaurant restaurant) =>
        new(
            restaurant.Name,
            ToDto(restaurant.Address),
            ToDto(restaurant.OpeningHours));

    private static AddressDto ToDto(Address address) =>
        new(
            address.Street,
            address.BuildingNumber,
            address.City,
            address.PostalCode,
            address.ApartmentNumber,
            address.Notes);

    public static OpeningHoursDto ToDto(OpeningHours openingHours)
    {
        var schedule = Enum.GetValues<DayOfWeek>().ToDictionary(
            day => day,
            day => (IReadOnlyList<TimeRangeDto>)openingHours.RangesFor(day)
                .Select(r => new TimeRangeDto(r.Start, r.End))
                .ToList());

        return new OpeningHoursDto(schedule);
    }

    public static OpeningHours ToDomain(OpeningHoursDto dto)
    {
        var schedule = dto.Schedule.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<TimeRange>)kv.Value.Select(r => new TimeRange(r.Start, r.End)).ToList());

        return new OpeningHours(schedule);
    }

    private static MoneyDto? ToDto(Money? money) =>
        money is null ? null : new MoneyDto(money.Amount, money.Currency);
}
