import { useParams } from 'react-router-dom'
import { OrderTrackingStatus } from '../components/orders/OrderTrackingStatus'
import { useOrderTracking } from '../hooks/useOrderTracking'

/**
 * Public route `/orders/track/:trackingToken` (ADR-0038) — lets a guest open a shareable,
 * unguessable link to their order's live status, independent of the sessionStorage used by
 * OrderConfirmationPage (which doesn't survive a new tab/device).
 */
export function TrackOrderPage() {
  const { trackingToken } = useParams<{ trackingToken: string }>()
  const tracking = useOrderTracking(trackingToken ? { trackingToken } : null)

  return (
    <div className="checkout-step">
      <h2>Śledzenie zamówienia</h2>

      {!trackingToken || tracking.error ? (
        <p className="empty-state">
          Nie znaleziono zamówienia o tym identyfikatorze. Sprawdź, czy link został skopiowany poprawnie.
        </p>
      ) : (
        <>
          {tracking.order && (
            <p>
              Numer zamówienia: <strong>{tracking.order.number}</strong>
            </p>
          )}
          <OrderTrackingStatus {...tracking} />
        </>
      )}
    </div>
  )
}
