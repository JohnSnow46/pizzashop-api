import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { updateOrderingThresholds } from '../../api/restaurantApi'
import type { Money } from '../../api/types'

interface OrderingThresholdsFormProps {
  minimumOrderValue: Money | null
  freeDeliveryThreshold: Money | null
  deliveryFee: Money
  onSaved: () => void
}

/** Edit form for order/delivery thresholds (PUT /api/restaurant/ordering-thresholds). */
export function OrderingThresholdsForm({
  minimumOrderValue,
  freeDeliveryThreshold,
  deliveryFee,
  onSaved,
}: OrderingThresholdsFormProps) {
  const currency = deliveryFee.currency
  const [hasMinimumOrder, setHasMinimumOrder] = useState(minimumOrderValue !== null)
  const [minimumOrderAmount, setMinimumOrderAmount] = useState(minimumOrderValue?.amount ?? 0)
  const [hasFreeDeliveryThreshold, setHasFreeDeliveryThreshold] = useState(freeDeliveryThreshold !== null)
  const [freeDeliveryAmount, setFreeDeliveryAmount] = useState(freeDeliveryThreshold?.amount ?? 0)
  const [deliveryFeeAmount, setDeliveryFeeAmount] = useState(deliveryFee.amount)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    try {
      await updateOrderingThresholds({
        minimumOrderValue: hasMinimumOrder ? { amount: minimumOrderAmount, currency } : null,
        freeDeliveryThreshold: hasFreeDeliveryThreshold ? { amount: freeDeliveryAmount, currency } : null,
        deliveryFee: { amount: deliveryFeeAmount, currency },
      })
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać progów zamówień.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>Progi zamówień i dostawy</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Opłata za dostawę ({currency})
          <input
            type="number"
            step="0.01"
            min="0"
            value={deliveryFeeAmount}
            onChange={(e) => setDeliveryFeeAmount(Number(e.target.value))}
            required
          />
        </label>

        <label className="checkout-field checkout-field--inline">
          <input type="checkbox" checked={hasMinimumOrder} onChange={(e) => setHasMinimumOrder(e.target.checked)} />
          Minimalna wartość zamówienia
        </label>
        {hasMinimumOrder && (
          <label className="checkout-field">
            Kwota minimalna ({currency})
            <input
              type="number"
              step="0.01"
              min="0"
              value={minimumOrderAmount}
              onChange={(e) => setMinimumOrderAmount(Number(e.target.value))}
            />
          </label>
        )}

        <label className="checkout-field checkout-field--inline">
          <input
            type="checkbox"
            checked={hasFreeDeliveryThreshold}
            onChange={(e) => setHasFreeDeliveryThreshold(e.target.checked)}
          />
          Próg darmowej dostawy
        </label>
        {hasFreeDeliveryThreshold && (
          <label className="checkout-field">
            Próg ({currency})
            <input
              type="number"
              step="0.01"
              min="0"
              value={freeDeliveryAmount}
              onChange={(e) => setFreeDeliveryAmount(Number(e.target.value))}
            />
          </label>
        )}
      </div>

      {fieldErrors && (
        <ul className="checkout-error-list">
          {Object.entries(fieldErrors).map(([field, messages]) => (
            <li key={field}>
              {field}: {messages.join(' ')}
            </li>
          ))}
        </ul>
      )}
      {submitError && <p className="checkout-error">{submitError}</p>}

      <div className="checkout-actions">
        <button type="submit" className="add-to-cart-btn" disabled={submitting}>
          {submitting ? 'Zapisywanie...' : 'Zapisz progi'}
        </button>
      </div>
    </form>
  )
}
