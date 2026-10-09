import { apiClient } from './client'
import type { AddCartItemCommand, CartDto } from './types'

/** GET /api/cart — the caller's own persistent cart (ADR-0043, Customer role). Empty cart and no cart are indistinguishable (empty CartDto). */
export function getMyCart(): Promise<CartDto> {
  return apiClient.get<CartDto>('/cart')
}

/** POST /api/cart/items — adds a line, or merges into an existing one with the same selection (ADR-0043 §B). Returns the resulting line's id. */
export function addCartItem(command: AddCartItemCommand): Promise<string> {
  return apiClient.post<string>('/cart/items', command)
}

/** PATCH /api/cart/items/{itemId} — quantity must be 1-100; use removeCartItem for 0/negative. */
export function updateCartItemQuantity(itemId: string, quantity: number): Promise<void> {
  return apiClient.patch<void>(`/cart/items/${itemId}`, { quantity })
}

export function removeCartItem(itemId: string): Promise<void> {
  return apiClient.delete<void>(`/cart/items/${itemId}`)
}

export function clearCart(): Promise<void> {
  return apiClient.delete<void>('/cart')
}
