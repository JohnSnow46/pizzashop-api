import { Link, useSearchParams } from 'react-router-dom'
import { loadOrderResult } from '../checkout/orderResultStorage'
import { OrderTrackingStatus } from '../components/orders/OrderTrackingStatus'
import { useOrderTracking, type OrderTrackingSource } from '../hooks/useOrderTracking'

/**
 * Confirmation page (ADR-0036) — a dedicated route (not part of the CheckoutPage wizard state)
 * because it must survive a full page reload on return from PayU's external domain
 * (PayU:ContinueUrl). Reads the last order result from sessionStorage, not the URL/state.
 *
 * Live-tracking (ADR-0038): guests are tracked via `guestTrackingToken` (also surfaced here as
 * a shareable `/orders/track/:trackingToken` link, since sessionStorage doesn't follow the guest
 * to another tab/device); logged-in customers are tracked via `orderId` instead.
 */
export function OrderConfirmationPage() {
  const [searchParams] = useSearchParams()
  const paymentError = searchParams.get('error')
  const result = loadOrderResult()

  const trackingSource: OrderTrackingSource | null = result
    ? result.guestTrackingToken
      ? { trackingToken: result.guestTrackingToken }
      : { orderId: result.orderId }
    : null
  const tracking = useOrderTracking(trackingSource)

  if (!result) {
    return (
      <div className="checkout-step">
        <h2>Brak informacji o zamówieniu</h2>
        <p className="empty-state">
          Nie znaleziono danych ostatniego zamówienia w tej przeglądarce. Jeśli właśnie złożyłeś zamówienie, wróć do
          menu i spróbuj ponownie.
        </p>
        <Link to="/">Wróć do menu</Link>
      </div>
    )
  }

  const trackingPath = result.guestTrackingToken ? `/orders/track/${result.guestTrackingToken}` : null

  return (
    <div className="checkout-step">
      <h2>Dziękujemy za zamówienie!</h2>
      <p>
        Numer zamówienia: <strong>{result.number}</strong>
      </p>

      {paymentError && (
        <p className="checkout-banner checkout-banner--error">
          Płatność nie została jeszcze potwierdzona (lub została anulowana). Zamówienie zostało zarejestrowane i
          poczeka na potwierdzenie płatności — możesz spróbować zapłacić ponownie lub skontaktować się z restauracją.
        </p>
      )}

      {trackingPath && (
        <p className="checkout-hint">
          Link do śledzenia zamówienia:{' '}
          <Link to={trackingPath}>{`${window.location.origin}${trackingPath}`}</Link>. Zachowaj go lub prześlij dalej
          — pozwala sprawdzić status zamówienia z dowolnej karty lub urządzenia.
        </p>
      )}

      <OrderTrackingStatus {...tracking} />

      <Link to="/">Wróć do menu</Link>
    </div>
  )
}
