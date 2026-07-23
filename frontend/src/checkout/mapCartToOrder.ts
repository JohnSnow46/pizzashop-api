import type { CreateOrderItem, PromotionDiscountLine } from '../api/types'
import type { CartItem } from '../cart/types'

/**
 * Maps cart lines onto CreateOrderItemDto (ADR-0036, Decision point 4). Prices are
 * deliberately NOT sent — CartItem's unitPriceAmount is orientation-only client-side display;
 * the server recomputes every price from MenuItemId/VariantId/ExtraIngredientIds.
 */
export function cartItemsToOrderItems(items: CartItem[]): CreateOrderItem[] {
  return items.map((item) => ({
    menuItemId: item.menuItemId,
    variantId: item.variantId,
    quantity: item.quantity,
    extraIngredientIds: item.extraIds,
    notes: null,
  }))
}

/**
 * Builds the line items needed by ValidatePromotionCodeQuery for BuyXGetY-style discount
 * previews (ADR-0034/ADR-0036). Uses the cart's orientation-only unit price — the real
 * discount is recomputed server-side when the order is created.
 */
export function cartItemsToPromotionLines(items: CartItem[]): PromotionDiscountLine[] {
  return items.map((item) => ({
    menuItemId: item.menuItemId,
    unitPrice: { amount: item.unitPriceAmount, currency: item.currency },
    quantity: item.quantity,
  }))
}
