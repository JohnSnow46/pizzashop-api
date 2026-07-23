import { apiClient } from './client'
import type { Address, CreateOrderCommand, CreateOrderResult, DeliveryAvailability } from './types'

/** POST /api/orders/check-delivery — flow step 2 (CLAUDE.md), checked before showing the cart. */
export function checkDelivery(address: Address): Promise<DeliveryAvailability> {
  return apiClient.post<DeliveryAvailability>('/orders/check-delivery', { address })
}

/** POST /api/orders — places a guest (or logged-in) order (ADR-0036). */
export function createOrder(command: CreateOrderCommand): Promise<CreateOrderResult> {
  return apiClient.post<CreateOrderResult>('/orders', command)
}
