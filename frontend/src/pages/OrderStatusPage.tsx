import { useParams } from 'react-router-dom'
import { OrderTrackingStatus } from '../components/orders/OrderTrackingStatus'
import { RetryPaymentButton } from '../components/orders/RetryPaymentButton'
import { useOrderTracking } from '../hooks/useOrderTracking'

/**
 * `/account/orders/:orderId` (ADR-0038 live tracking, ADR-0039 `/account`) — logged-in-customer
 * analog of the guest `/orders/track/:trackingToken` route: a durable, bookmarkable link to a
 * single order's live status, reached from the order history list on `/account`.
 */
export function OrderStatusPage() {
  const { orderId } = useParams<{ orderId: string }>()
  const tracking = useOrderTracking(orderId ? { orderId } : null)

  return (
    <div className="checkout-step">
      <h2>Status zamówienia</h2>

      {!orderId || tracking.error ? (
        <p className="empty-state">Nie znaleziono zamówienia.</p>
      ) : (
        <>
          {tracking.order && (
            <p>
              Numer zamówienia: <strong>{tracking.order.number}</strong>
            </p>
          )}
          <OrderTrackingStatus {...tracking} />
          {orderId && tracking.order?.paymentMethod === 'Online' && tracking.order.paymentStatus !== 'Paid' && (
            <RetryPaymentButton source={{ orderId }} />
          )}
        </>
      )}
    </div>
  )
}
