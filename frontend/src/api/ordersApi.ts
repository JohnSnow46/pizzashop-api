import { apiClient } from './client'
import type { Address, CreateOrderCommand, CreateOrderResult, DeliveryAvailability, Order, OrderSummary } from './types'

/** POST /api/orders/check-delivery — flow step 2 (CLAUDE.md), checked before showing the cart. */
export function checkDelivery(address: Address): Promise<DeliveryAvailability> {
  return apiClient.post<DeliveryAvailability>('/orders/check-delivery', { address })
}

/** POST /api/orders — places a guest (or logged-in) order (ADR-0036). */
export function createOrder(command: CreateOrderCommand): Promise<CreateOrderResult> {
  return apiClient.post<CreateOrderResult>('/orders', command)
}

/**
 * GET /api/orders/{id} — initial REST fetch for live-tracking a logged-in customer's own
 * order (ADR-0038). Requires JWT ([Authorize]); apiClient attaches it automatically.
 */
export function getOrderById(orderId: string): Promise<Order> {
  return apiClient.get<Order>(`/orders/${orderId}`)
}

/**
 * GET /api/orders/track/{trackingToken} — initial REST fetch for live-tracking a guest order
 * (ADR-0038). [AllowAnonymous]; the token is the unguessable identifier from CreateOrderResult.
 */
export function getOrderByTrackingToken(trackingToken: string): Promise<Order> {
  return apiClient.get<Order>(`/orders/track/${trackingToken}`)
}

/** GET /api/orders/mine — logged-in customer's own order history, newest first (ADR-0039). */
export function getMyOrders(): Promise<OrderSummary[]> {
  return apiClient.get<OrderSummary[]>('/orders/mine')
}
