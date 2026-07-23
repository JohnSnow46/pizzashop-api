import { Link, useSearchParams } from 'react-router-dom'
import { loadOrderResult } from '../checkout/orderResultStorage'

/**
 * Confirmation page (ADR-0036) — a dedicated route (not part of the CheckoutPage wizard state)
 * because it must survive a full page reload on return from PayU's external domain
 * (PayU:ContinueUrl). Reads the last order result from sessionStorage, not the URL/state.
 */
export function OrderConfirmationPage() {
  const [searchParams] = useSearchParams()
  const paymentError = searchParams.get('error')
  const result = loadOrderResult()

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

      {result.guestTrackingToken && (
        <p className="checkout-hint">
          Twój identyfikator śledzenia zamówienia: <code>{result.guestTrackingToken}</code>. Zachowaj go, aby
          sprawdzić status zamówienia (śledzenie na żywo pojawi się w kolejnej iteracji).
        </p>
      )}

      <Link to="/">Wróć do menu</Link>
    </div>
  )
}
