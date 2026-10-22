import type { OrderItem } from '../../api/types'

/** "2× Margherita — 32 cm (pieczarki, oliwki)" — same shape OrderSummary uses for cart lines
 * at checkout, applied to a placed order's items. */
export function formatOrderItem(item: OrderItem): string {
  const variant = item.variantName ? ` — ${item.variantName}` : ''
  const extras = item.extras.length > 0 ? ` (${item.extras.map((e) => e.name).join(', ')})` : ''
  return `${item.quantity}× ${item.menuItemName}${variant}${extras}`
}
