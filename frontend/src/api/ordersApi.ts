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

/**
 * GET /api/orders/queue — staff order queue (Employee/RestaurantAdmin/SuperAdmin), all
 * non-terminal statuses sorted oldest-first (docs/api-layer.md 6.6).
 */
export function getOrderQueue(): Promise<Order[]> {
  return apiClient.get<Order[]>('/orders/queue')
}

/** POST /api/orders/{id}/accept — staff accepts a PendingAcceptance order. */
export function acceptOrder(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/accept`, {})
}

/** POST /api/orders/{id}/reject — staff rejects a PendingAcceptance order. */
export function rejectOrder(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/reject`, {})
}

/** POST /api/orders/{id}/cancel — staff cancels an order (ADR-0018: refunds synchronously if paid). */
export function cancelOrder(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/cancel`, {})
}

/** POST /api/orders/{id}/start-preparation — staff moves an Accepted order into preparation. */
export function startPreparation(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/start-preparation`, {})
}

/** POST /api/orders/{id}/mark-ready — staff marks an InPreparation order as Ready. */
export function markReady(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/mark-ready`, {})
}

/** POST /api/orders/{id}/start-delivery — staff sends a Ready delivery order out for delivery. */
export function startDelivery(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/start-delivery`, {})
}

/** POST /api/orders/{id}/complete — staff completes a Ready (pickup) or OutForDelivery order. */
export function completeOrder(orderId: string): Promise<void> {
  return apiClient.post<void>(`/orders/${orderId}/complete`, {})
}
