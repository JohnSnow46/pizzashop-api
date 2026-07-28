import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import type { NewOrderPlacedEvent } from '../api/types'
import { useAuth } from './useAuth'

/** Same hub as useOrderTracking (ADR-0038 extension) — the staff queue reuses the one hub route. */
const HUB_URL = '/hubs/order-tracking'

/**
 * Live "new order" push for the staff queue (ADR-0038 extension). Connects to
 * OrderTrackingHub and calls `SubscribeToStaffQueue` — the hub verifies the caller's role
 * (Employee/RestaurantAdmin/SuperAdmin) from the JWT before joining the "staff" broadcast
 * group, so a non-staff or unauthenticated connection is simply never subscribed (no error
 * surfaces here either way, matching the rest of this hub — ADR-0028).
 *
 * `onNewOrder` fires once per "NewOrderPlaced" push with the new order's id; the page decides
 * what to do with it (add to the queue, bump a counter, play a sound).
 */
export function useEmployeeOrderQueueLive(onNewOrder: (orderId: string) => void) {
  const { token } = useAuth()
  const onNewOrderRef = useRef(onNewOrder)
  onNewOrderRef.current = onNewOrder

  useEffect(() => {
    if (!token) return

    let cancelled = false
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build()

    connection.on('NewOrderPlaced', (payload: NewOrderPlacedEvent) => {
      onNewOrderRef.current(payload.orderId)
    })

    // SignalR group membership doesn't survive a reconnect, so re-subscribe every time.
    connection.onreconnected(() => {
      void connection.invoke('SubscribeToStaffQueue').catch(() => {})
    })

    async function subscribe() {
      try {
        await connection.start()
        if (cancelled) return
        await connection.invoke('SubscribeToStaffQueue')
      } catch {
        // Silent — same reasoning as useOrderTracking (ADR-0028): the queue still works via
        // the initial REST fetch and per-row polling, just without the live "new order" push.
      }
    }

    void subscribe()

    return () => {
      cancelled = true
      void connection.stop()
    }
  }, [token])
}
