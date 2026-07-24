import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { updateDeliveryArea } from '../../api/restaurantApi'
import type { GeoCoordinate } from '../../api/types'

interface DeliveryAreaFormProps {
  location: GeoCoordinate
  deliveryRadiusKm: number
  onSaved: () => void
}

/** Edit form for the delivery area — center point + radius (PUT /api/restaurant/delivery-area). */
export function DeliveryAreaForm({ location, deliveryRadiusKm, onSaved }: DeliveryAreaFormProps) {
  const [latitude, setLatitude] = useState(location.latitude)
  const [longitude, setLongitude] = useState(location.longitude)
  const [radiusKm, setRadiusKm] = useState(deliveryRadiusKm)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    try {
      await updateDeliveryArea({ latitude, longitude, deliveryRadiusKm: radiusKm })
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać obszaru dostawy.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>Obszar dostawy</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Szerokość geograficzna
          <input
            type="number"
            step="0.000001"
            min="-90"
            max="90"
            value={latitude}
            onChange={(e) => setLatitude(Number(e.target.value))}
            required
          />
        </label>

        <label className="checkout-field">
          Długość geograficzna
          <input
            type="number"
            step="0.000001"
            min="-180"
            max="180"
            value={longitude}
            onChange={(e) => setLongitude(Number(e.target.value))}
            required
          />
        </label>

        <label className="checkout-field">
          Promień dostawy (km)
          <input
            type="number"
            step="0.1"
            min="0.1"
            value={radiusKm}
            onChange={(e) => setRadiusKm(Number(e.target.value))}
            required
          />
        </label>
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
          {submitting ? 'Zapisywanie...' : 'Zapisz obszar dostawy'}
        </button>
      </div>
    </form>
  )
}
