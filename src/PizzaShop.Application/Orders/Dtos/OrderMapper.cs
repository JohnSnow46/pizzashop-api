using PizzaShop.Application.Common.Dtos;
using PizzaShop.Domain.Orders;
using PizzaShop.Domain.ValueObjects;

namespace PizzaShop.Application.Orders.Dtos;

internal static class OrderMapper
{
    public static OrderDto ToDto(Order order) =>
        new(
            order.Id,
            order.Number,
            order.CustomerId,
            new ContactDetailsDto(order.Contact.FullName, order.Contact.PhoneNumber, order.Contact.Email),
            order.FulfillmentType,
            ToDto(order.DeliveryAddress?.Address),
            order.PlacedAt,
            order.RequestedFulfillmentTime,
            order.EstimatedReadyAt,
            order.Status,
            order.PaymentMethod,
            order.PaymentStatus,
            ToDto(order.Subtotal)!,
            ToDto(order.DiscountAmount)!,
            ToDto(order.DeliveryFee)!,
            ToDto(order.Total)!,
            order.Items.Select(ToDto).ToList());

    private static OrderItemDto ToDto(OrderItem item) =>
        new(
            item.Id,
            item.MenuItemId,
            item.MenuItemName,
            item.VariantId,
            item.VariantName,
            ToDto(item.UnitPrice)!,
            item.Quantity,
            item.Notes,
            item.Extras.Select(e => new OrderItemExtraDto(e.IngredientId, e.Name, ToDto(e.Price)!)).ToList(),
            ToDto(item.LineTotal)!);

    private static AddressDto? ToDto(Address? address) =>
        address is null
            ? null
            : new AddressDto(
                address.Street,
                address.BuildingNumber,
                address.City,
                address.PostalCode,
                address.ApartmentNumber,
                address.Notes);

    private static MoneyDto? ToDto(Money? money) =>
        money is null ? null : new MoneyDto(money.Amount, money.Currency);
}
