import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { useEffect, useState } from 'react'
import { getOrderById, getOrderByTrackingToken } from '../api/ordersApi'
import type { Order, OrderStatusChangedEvent } from '../api/types'
import { useAuth } from './useAuth'

/**
 * Same-origin relative URL to OrderTrackingHub (ADR-0038, Decision 2) — mirrors BASE_URL in
 * api/client.ts. In dev, vite.config.ts proxies /hubs/* (with ws: true) to the PizzaShop.Api
 * dev server; in production the frontend must be served from the same origin as the Api.
 */
const HUB_URL = '/hubs/order-tracking'

export type OrderTrackingSource = { orderId: string } | { trackingToken: string }

export type OrderTrackingConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export interface UseOrderTrackingResult {
  /** Full OrderDto (REST). status/estimatedReadyAt are updated live from the Hub push; the rest
   * stays as fetched (doesn't change over the tracking session). */
  order: Order | null
  /** True while the initial REST fetch is in flight. */
  isLoading: boolean
  /** REST error (404/network). Hub errors are deliberately silent (ADR-0028). */
  error: string | null
  connectionState: OrderTrackingConnectionState
}

/**
 * Live-tracks a single order's status via OrderTrackingHub (ADR-0038, Decision 5). The variant
 * chosen (orderId vs. trackingToken) determines both the REST endpoint (getOrderById /
 * getOrderByTrackingToken) and the Hub method invoked (SubscribeToOrder / SubscribeToGuestOrder)
 * — the same two paths the backend exposes, no third "guessed" combination.
 *
 * One hook = one HubConnection, created and torn down with the calling component's lifecycle
 * (no module-level singleton, Decision 3) — fine for this iteration where a single screen tracks
 * a single order at a time.
 *
 * `source: null` means "nothing to track yet" (e.g. no order result in sessionStorage, no route
 * param) — skips the REST fetch and Hub connection entirely instead of firing a request that's
 * known upfront to 404.
 */
export function useOrderTracking(source: OrderTrackingSource | null): UseOrderTrackingResult {
  const { token } = useAuth()
  const [order, setOrder] = useState<Order | null>(null)
  const [isLoading, setIsLoading] = useState(source !== null)
  const [error, setError] = useState<string | null>(null)
  const [connectionState, setConnectionState] = useState<OrderTrackingConnectionState>('connecting')

  const sourceKey = source === null ? null : 'orderId' in source ? `order:${source.orderId}` : `token:${source.trackingToken}`

  useEffect(() => {
    if (source === null) {
      setOrder(null)
      setIsLoading(false)
      setError(null)
      setConnectionState('disconnected')
      return
    }

    let cancelled = false
    let connection: HubConnection | null = null

    setOrder(null)
    setIsLoading(true)
    setError(null)
    setConnectionState('connecting')

    const initialFetch = 'orderId' in source ? getOrderById(source.orderId) : getOrderByTrackingToken(source.trackingToken)

    initialFetch
      .then((fetchedOrder) => {
        if (cancelled) return

        setOrder(fetchedOrder)
        setIsLoading(false)

        connection = new HubConnectionBuilder()
          .withUrl(HUB_URL, token ? { accessTokenFactory: () => token } : {})
          .withAutomaticReconnect()
          .build()

        connection.on('OrderStatusChanged', (payload: OrderStatusChangedEvent) => {
          setOrder((prev) =>
            prev && prev.id === payload.orderId
              ? { ...prev, status: payload.status, estimatedReadyAt: payload.estimatedReadyAt }
              : prev,
          )
        })

        connection.onreconnecting(() => setConnectionState('reconnecting'))
        connection.onreconnected(() => setConnectionState('connected'))
        connection.onclose(() => setConnectionState('disconnected'))

        return connection
          .start()
          .then(() => {
            if (cancelled || !connection) return
            setConnectionState('connected')
            return 'orderId' in source
              ? connection.invoke('SubscribeToOrder', source.orderId)
              : connection.invoke('SubscribeToGuestOrder', source.trackingToken)
          })
          .catch(() => {
            // Hub connection/subscription failures are silent (ADR-0028) — the UI falls back
            // to whatever was last fetched over REST.
            if (!cancelled) setConnectionState('disconnected')
          })
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setIsLoading(false)
        setError(err instanceof Error ? err.message : 'Nie udało się pobrać danych zamówienia.')
      })

    return () => {
      cancelled = true
      void connection?.stop()
    }
    // `sourceKey` (not `source`) is the dependency on purpose: it's the stable, primitive
    // representation of `source`; re-running on `source`'s object identity would reconnect on
    // every render, since callers pass a fresh object literal each time.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sourceKey, token])

  return { order, isLoading, error, connectionState }
}
