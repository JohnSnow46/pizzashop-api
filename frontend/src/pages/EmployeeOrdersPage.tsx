import { useEffect, useState } from 'react'
import { getOrderQueue } from '../api/ordersApi'
import { EmployeeOrderRow } from '../components/orders/EmployeeOrderRow'

/**
 * `/employee/orders` (RequireAuth roles=Staff) — live order queue for staff. Each row tracks
 * its own order via `useOrderTracking`/SignalR; this page only owns the *set* of order ids in
 * the queue. The Hub has no "new order arrived" broadcast (ADR-0038), so new orders are only
 * discovered by refetching the queue — triggered here whenever a row leaves it (accepted →
 * ... → completed/rejected), which is also the moment a newly placed order is likely to show up.
 */
export function EmployeeOrdersPage() {
  const [orderIds, setOrderIds] = useState<string[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

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

  return (
    <div className="checkout-step">
      <h2>Kolejka zamówień</h2>

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
