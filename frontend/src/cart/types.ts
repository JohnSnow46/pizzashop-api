/**
 * A single cart line: a menu item with a chosen variant (if the item has variants) and a set
 * of chosen extras. Cart-only concept (ADR-0035, point 4) — never sent to the Api as-is; at
 * checkout time (future iteration) this maps onto CreateOrderCommand, which recomputes prices
 * from scratch server-side. `key` uniquely identifies the (menuItemId, variantId, extras)
 * combination so adding the same combination twice increments quantity instead of duplicating
 * the line.
 */
export interface CartItem {
  key: string
  menuItemId: string
  menuItemName: string
  variantId: string | null
  variantName: string | null
  extraIds: string[]
  extraNames: string[]
  /**
   * Orientation-only unit price, client-side display only: the selected variant's price if
   * the item has variants (variant price REPLACES basePrice, it is not added to it), falling
   * back to basePrice when there are no variants, plus the sum of the selected extras' prices.
   * See MenuItemCard.tsx for the actual computation.
   */
  unitPriceAmount: number
  currency: string
  quantity: number
}

export function buildCartItemKey(menuItemId: string, variantId: string | null, extraIds: string[]): string {
  const sortedExtras = [...extraIds].sort().join(',')
  return `${menuItemId}|${variantId ?? ''}|${sortedExtras}`
}
