import { useEffect, useRef, useState } from 'react'
import { getOrderQueue } from '../api/ordersApi'
import { NEW_ORDER_BEEP_SRC } from '../assets/newOrderBeep'
import { EmployeeOrderRow } from '../components/orders/EmployeeOrderRow'
import { useEmployeeOrderQueueLive } from '../hooks/useEmployeeOrderQueueLive'

/**
 * `/employee/orders` (RequireAuth roles=Staff) — live order queue for staff. Each row tracks
 * its own order via `useOrderTracking`/SignalR; this page only owns the *set* of order ids in
 * the queue. New orders arrive live via `useEmployeeOrderQueueLive` ("NewOrderPlaced" push,
 * ADR-0038 extension) and are appended to the queue immediately; the queue is also refetched
 * whenever a row leaves it (accepted → ... → completed/rejected) as a fallback in case a push
 * was missed while disconnected.
 */
export function EmployeeOrdersPage() {
  const [orderIds, setOrderIds] = useState<string[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [newOrderCount, setNewOrderCount] = useState(0)
  const audioRef = useRef<HTMLAudioElement | null>(null)

  useEffect(() => {
    let cancelled = false

    setIsLoading(true)
    setError(null)

    getOrderQueue()
      .then((orders) => {
        if (cancelled) return
        setOrderIds(orders.map((order) => order.id))
        setIsLoading(false)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Nie udało się pobrać kolejki zamówień.')
        setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  async function refetchQueue() {
    try {
      const fresh = await getOrderQueue()
      setOrderIds((prev) => {
        const prevIds = prev ?? []
        const prevSet = new Set(prevIds)
        const merged = [...prevIds]
        for (const order of fresh) {
          if (!prevSet.has(order.id)) merged.push(order.id)
        }
        return merged
      })
    } catch {
      // Silent — the queue keeps showing what it already has; the next leave-queue event retries.
    }
  }

  function handleLeftQueue(orderId: string) {
    setOrderIds((prev) => (prev ? prev.filter((id) => id !== orderId) : prev))
    void refetchQueue()
  }

  function handleNewOrder(orderId: string) {
    setOrderIds((prev) => {
      if (prev && prev.includes(orderId)) return prev
      return [...(prev ?? []), orderId]
    })
    setNewOrderCount((count) => count + 1)
    audioRef.current?.play().catch(() => {
      // Autoplay can be blocked until the user interacts with the page — silently skip the beep.
    })
  }

  useEmployeeOrderQueueLive(handleNewOrder)

  return (
    <div className="checkout-step">
      <audio ref={audioRef} src={NEW_ORDER_BEEP_SRC} preload="auto" />

      <div className="employee-orders-header">
        <h2>Kolejka zamówień</h2>
        {newOrderCount > 0 && (
          <button
            type="button"
            className="employee-orders-new-badge"
            onClick={() => setNewOrderCount(0)}
            title="Kliknij, aby wyzerować licznik"
          >
            {newOrderCount} {newOrderCount === 1 ? 'nowe zamówienie' : 'nowych zamówień'}
          </button>
        )}
      </div>

      {isLoading && <p>Ładowanie kolejki...</p>}
      {error && <p className="empty-state">{error}</p>}

      {!isLoading && !error && (
        orderIds && orderIds.length > 0 ? (
          <ul className="account-order-list">
            {orderIds.map((orderId) => (
              <EmployeeOrderRow key={orderId} orderId={orderId} onLeftQueue={handleLeftQueue} />
            ))}
          </ul>
        ) : (
          <p className="empty-state">Brak zamówień w kolejce.</p>
        )
      )}
    </div>
  )
}
