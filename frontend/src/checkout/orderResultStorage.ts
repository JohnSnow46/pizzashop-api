import type { CreateOrderResult } from '../api/types'

/**
 * Channel to hand CreateOrderResultDto to the confirmation page (ADR-0036, Decision point 2).
 * sessionStorage is used (not React Router navigate(state)) because it also survives the full
 * page reload that happens when PayU redirects back to PayU:ContinueUrl.
 */
const STORAGE_KEY = 'pizzashop.lastOrder'

export function saveOrderResult(result: CreateOrderResult): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(result))
}

export function loadOrderResult(): CreateOrderResult | null {
  const raw = sessionStorage.getItem(STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as CreateOrderResult
  } catch {
    return null
  }
}

export function clearOrderResult(): void {
  sessionStorage.removeItem(STORAGE_KEY)
}
