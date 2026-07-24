import { useEffect, useState } from 'react'
import { getMyOrders } from '../api/ordersApi'
import { getLoyaltyBalance } from '../api/loyaltyApi'
import type { FulfillmentType, LoyaltyBalance, LoyaltyTransactionType, OrderSummary, PaymentStatus } from '../api/types'
import { STATUS_LABELS } from '../components/orders/orderStatusLabels'

const FULFILLMENT_LABELS: Record<FulfillmentType, string> = {
  Delivery: 'Dostawa',
  Pickup: 'Odbiór osobisty',
}

const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Pending: 'Oczekuje',
  Authorized: 'Autoryzowana',
  Paid: 'Opłacona',
  Refunded: 'Zwrócona',
  Failed: 'Nieudana',
}

const LOYALTY_TYPE_LABELS: Record<LoyaltyTransactionType, string> = {
  Earned: 'Naliczono',
  Redeemed: 'Wykorzystano',
  Adjusted: 'Korekta',
  Expired: 'Wygasło',
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('pl-PL')
}

function formatMoney(amount: number, currency: string): string {
  return `${amount.toFixed(2)} ${currency}`
}

/**
 * `/account` (ADR-0039, RequireAuth-gated) — logged-in customer's order history + loyalty
 * points balance/history. Deliberately no link to a per-order detail view: the app has no
 * logged-in-customer order detail route yet (only the guest `/orders/track/:token` route and
 * the live-tracking view reachable right after checkout), so adding one is out of scope here.
 */
export function MyAccountPage() {
  const [orders, setOrders] = useState<OrderSummary[] | null>(null)
  const [loyalty, setLoyalty] = useState<LoyaltyBalance | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    setIsLoading(true)
    setError(null)

    Promise.all([getMyOrders(), getLoyaltyBalance()])
      .then(([myOrders, myLoyalty]) => {
        if (cancelled) return
        setOrders(myOrders)
        setLoyalty(myLoyalty)
        setIsLoading(false)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Nie udało się pobrać danych konta.')
        setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  if (isLoading) {
    return <p>Ładowanie konta...</p>
  }

  if (error) {
    return <p className="empty-state">{error}</p>
  }

  return (
    <div className="checkout-step">
      <h2>Moje konto</h2>

      <section>
        <h3>Historia zamówień</h3>
        {orders && orders.length > 0 ? (
          <ul className="account-order-list">
            {orders.map((order) => (
              <li key={order.id} className="account-order-list__item">
                <div className="account-order-list__details">
                  <strong>{order.number}</strong>
                  <span className="cart-item__meta">
                    {formatDateTime(order.placedAt)} · {FULFILLMENT_LABELS[order.fulfillmentType]} · {order.itemsCount}{' '}
                    poz.
                  </span>
                  <span className="cart-item__meta">
                    {STATUS_LABELS[order.status]} · Płatność: {PAYMENT_STATUS_LABELS[order.paymentStatus]}
                  </span>
                </div>
                <div>{formatMoney(order.total.amount, order.total.currency)}</div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty-state">Nie masz jeszcze żadnych zamówień.</p>
        )}
      </section>

      <section>
        <h3>Punkty lojalnościowe</h3>
        <p>
          Saldo: <strong>{loyalty?.pointsBalance ?? 0} pkt</strong>
        </p>
        {loyalty && loyalty.transactions.length > 0 ? (
          <ul className="account-order-list">
            {loyalty.transactions.map((transaction, index) => (
              <li key={index} className="account-order-list__item">
                <div className="account-order-list__details">
                  <strong>{LOYALTY_TYPE_LABELS[transaction.type]}</strong>
                  <span className="cart-item__meta">{transaction.reason}</span>
                  <span className="cart-item__meta">{formatDateTime(transaction.occurredAt)}</span>
                </div>
                <div>{transaction.points > 0 ? `+${transaction.points}` : transaction.points} pkt</div>
              </li>
            ))}
          </ul>
        ) : (
          <p className="empty-state">Brak historii punktów.</p>
        )}
      </section>
    </div>
  )
}
