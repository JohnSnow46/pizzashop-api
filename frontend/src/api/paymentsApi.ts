import { apiClient } from './client'
import type { InitializePaymentResult } from './types'

/**
 * POST /api/payments/orders/{orderId}/initialize — (re-)initializes an online payment session
 * for an order owned by the logged-in customer (or staff acting on any order). Requires JWT;
 * apiClient attaches it automatically.
 */
export function initializePayment(orderId: string): Promise<InitializePaymentResult> {
  return apiClient.post<InitializePaymentResult>(`/payments/orders/${orderId}/initialize`, {})
}

/**
 * POST /api/payments/orders/track/{trackingToken}/initialize — guest counterpart of
 * {@link initializePayment} (ADR-0041). [AllowAnonymous]; possession of the unguessable
 * tracking token is the access control, mirroring `getOrderByTrackingToken`.
 */
export function initializeGuestPayment(trackingToken: string): Promise<InitializePaymentResult> {
  return apiClient.post<InitializePaymentResult>(`/payments/orders/track/${trackingToken}/initialize`, {})
}
