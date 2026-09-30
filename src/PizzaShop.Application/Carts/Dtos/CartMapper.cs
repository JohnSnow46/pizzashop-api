using PizzaShop.Domain.Carts;

namespace PizzaShop.Application.Carts.Dtos;

public static class CartMapper
{
    public static CartDto ToDto(Cart? cart) =>
        cart is null
            ? new CartDto([], DateTimeOffset.MinValue)
            : new CartDto(cart.Items.Select(ToDto).ToArray(), cart.UpdatedAt);

    private static CartItemDto ToDto(CartItem item) =>
        new(item.Id, item.MenuItemId, item.VariantId, item.ExtraIds, item.Quantity, item.Notes);
}
