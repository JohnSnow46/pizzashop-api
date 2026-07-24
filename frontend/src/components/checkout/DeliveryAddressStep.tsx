import { useEffect, useState } from 'react'
import { ApiError } from '../../api/client'
import { checkDelivery } from '../../api/ordersApi'
import { getMyAddresses } from '../../api/customersApi'
import type { Address, CustomerAddress, DeliveryAvailability } from '../../api/types'
import { validateAddress } from '../../checkout/validation'
import { useAuth } from '../../hooks/useAuth'

const EMPTY_ADDRESS: Address = {
  street: '',
  buildingNumber: '',
  city: '',
  postalCode: '',
  apartmentNumber: null,
  notes: null,
}

interface DeliveryAddressStepProps {
  address: Address | null
  deliveryCheck: DeliveryAvailability | null
  onChecked: (address: Address, result: DeliveryAvailability) => void
  onSwitchToPickup: () => void
  onNext: () => void
  onBack: () => void
}

/**
 * Checkout step 2 (Delivery only, CLAUDE.md flow step 2): collects the delivery address and
 * checks it against the restaurant's delivery radius before letting the customer continue.
 */
export function DeliveryAddressStep({
  address,
  deliveryCheck,
  onChecked,
  onSwitchToPickup,
  onNext,
  onBack,
}: DeliveryAddressStepProps) {
  const { isAuthenticated, user } = useAuth()
  const [form, setForm] = useState<Address>(address ?? EMPTY_ADDRESS)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [checking, setChecking] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)
  const [result, setResult] = useState<DeliveryAvailability | null>(deliveryCheck)
  const [savedAddresses, setSavedAddresses] = useState<CustomerAddress[]>([])

  const isEligibleForSavedAddresses = isAuthenticated && user?.role === 'Customer'

  useEffect(() => {
    if (!isEligibleForSavedAddresses) {
      return
    }

    let cancelled = false
    getMyAddresses()
      .then((addresses) => {
        if (!cancelled) setSavedAddresses(addresses)
      })
      .catch(() => {
        if (!cancelled) setSavedAddresses([])
      })
    return () => {
      cancelled = true
    }
  }, [isEligibleForSavedAddresses])

  function updateField<K extends keyof Address>(field: K, value: Address[K]) {
    setForm((prev) => ({ ...prev, [field]: value }))
    setResult(null)
  }

  function applySavedAddress(entry: CustomerAddress) {
    setForm(entry.address)
    setErrors({})
    setResult(null)
  }

  async function handleCheck() {
    const validationErrors = validateAddress(form)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setChecking(true)
    setApiError(null)
    try {
      const availability = await checkDelivery(form)
      setResult(availability)
      onChecked(form, availability)
    } catch (err) {
      setApiError(err instanceof ApiError ? (err.detail ?? err.message) : 'Nie udało się sprawdzić obszaru dostawy.')
    } finally {
      setChecking(false)
    }
  }

  return (
    <div className="checkout-step">
      <h3>Adres dostawy</h3>

      {isEligibleForSavedAddresses && savedAddresses.length > 0 && (
        <div className="checkout-field">
          <span>Zapisane adresy</span>
          <div className="queue-actions">
            {savedAddresses.map((entry) => (
              <button type="button" key={entry.id} className="queue-action-btn" onClick={() => applySavedAddress(entry)}>
                {entry.label}
                {entry.isDefault ? ' (domyślny)' : ''}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Ulica
          <input value={form.street} onChange={(e) => updateField('street', e.target.value)} />
          {errors.street && <span className="checkout-error">{errors.street}</span>}
        </label>

        <label className="checkout-field">
          Numer budynku
          <input value={form.buildingNumber} onChange={(e) => updateField('buildingNumber', e.target.value)} />
          {errors.buildingNumber && <span className="checkout-error">{errors.buildingNumber}</span>}
        </label>

        <label className="checkout-field">
          Numer lokalu (opcjonalnie)
          <input
            value={form.apartmentNumber ?? ''}
            onChange={(e) => updateField('apartmentNumber', e.target.value || null)}
          />
        </label>

        <label className="checkout-field">
          Miasto
          <input value={form.city} onChange={(e) => updateField('city', e.target.value)} />
          {errors.city && <span className="checkout-error">{errors.city}</span>}
        </label>

        <label className="checkout-field">
          Kod pocztowy
          <input
            placeholder="00-001"
            value={form.postalCode}
            onChange={(e) => updateField('postalCode', e.target.value)}
          />
          {errors.postalCode && <span className="checkout-error">{errors.postalCode}</span>}
        </label>

        <label className="checkout-field">
          Uwagi dla kuriera (opcjonalnie)
          <input value={form.notes ?? ''} onChange={(e) => updateField('notes', e.target.value || null)} />
        </label>
      </div>

      {apiError && <p className="checkout-banner checkout-banner--error">{apiError}</p>}

      {result && !result.isAvailable && (
        <div className="checkout-banner checkout-banner--error">
          <p>Ten adres jest poza obszarem dostawy.</p>
          <button type="button" onClick={onSwitchToPickup}>
            Wybierz odbiór osobisty
          </button>
        </div>
      )}

      {result?.isAvailable && (
        <p className="checkout-banner checkout-banner--success">
          Adres w obszarze dostawy{result.distanceKm !== null ? ` (${result.distanceKm.toFixed(1)} km)` : ''}.
          {result.deliveryFee && ` Koszt dostawy: ${result.deliveryFee.amount.toFixed(2)} ${result.deliveryFee.currency}.`}
        </p>
      )}

      <div className="checkout-actions">
        <button type="button" onClick={onBack}>
          Wstecz
        </button>
        {result?.isAvailable ? (
          <button type="button" className="add-to-cart-btn" onClick={onNext}>
            Dalej
          </button>
        ) : (
          <button type="button" className="add-to-cart-btn" disabled={checking} onClick={handleCheck}>
            {checking ? 'Sprawdzanie...' : 'Sprawdź dostępność'}
          </button>
        )}
      </div>
    </div>
  )
}
