import type { CartItem } from './types'

const STORAGE_KEY = 'pizzashop.cart'

export function loadCart(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return []
    }
    const parsed: unknown = JSON.parse(raw)
    return Array.isArray(parsed) ? (parsed as CartItem[]) : []
  } catch {
    // Corrupt/unavailable localStorage should never break the app — start with an empty cart.
    return []
  }
}

export function saveCart(items: CartItem[]): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items))
  } catch {
    // localStorage may be unavailable (private mode, quota) — silently ignore, cart just
    // won't survive a refresh in that case.
  }
}
