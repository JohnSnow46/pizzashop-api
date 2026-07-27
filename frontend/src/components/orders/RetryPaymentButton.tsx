import { useState } from 'react'
import { ApiError } from '../../api/client'
import { initializeGuestPayment, initializePayment } from '../../api/paymentsApi'
import type { OrderTrackingSource } from '../../hooks/useOrderTracking'

interface RetryPaymentButtonProps {
  /** Same shape as `useOrderTracking`'s source — determines which endpoint/client is used. */
  source: OrderTrackingSource
}

/**
 * Retry-payment CTA shared by `OrderConfirmationPage` (payment failed/abandoned right after
 * checkout) and `TrackOrderPage` (guest returns later via the tracking link) — ADR-0041. Asks
 * the backend for a fresh PayU checkout URL and redirects the same way `CheckoutPage` does for
 * a brand-new order (`window.location.href = redirectUrl`), just via a stand-alone
 * Initialize(Guest)Payment call instead of `CreateOrderCommand`.
 */
export function RetryPaymentButton({ source }: RetryPaymentButtonProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleClick() {
    setIsSubmitting(true)
    setError(null)

    try {
      const result = 'orderId' in source ? await initializePayment(source.orderId) : await initializeGuestPayment(source.trackingToken)
      window.location.href = result.redirectUrl
    } catch (err) {
      setError(err instanceof ApiError ? (err.detail ?? err.title ?? err.message) : 'Nie udało się zainicjować płatności. Spróbuj ponownie.')
      setIsSubmitting(false)
    }
  }

  return (
    <div className="checkout-step">
      <button type="button" className="add-to-cart-btn" disabled={isSubmitting} onClick={handleClick}>
        {isSubmitting ? 'Przekierowywanie do płatności...' : 'Zapłać ponownie'}
      </button>
      {error && <p className="checkout-error">{error}</p>}
    </div>
  )
}
