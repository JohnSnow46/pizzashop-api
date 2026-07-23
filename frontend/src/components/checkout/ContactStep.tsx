import { useEffect, useState } from 'react'
import type { ContactDetails } from '../../api/types'
import { validateContact } from '../../checkout/validation'
import { useAuth } from '../../hooks/useAuth'

interface ContactStepProps {
  contact: ContactDetails
  onChange: (contact: ContactDetails) => void
  onNext: () => void
  onBack: () => void
}

/**
 * Checkout step 3: guest/customer contact details (CreateOrderCommand.Contact). When logged in
 * (ADR-0037), fullName/email are prefilled from the account once (fields stay editable) — phone
 * is not prefilled since AuthResultDto doesn't carry it.
 */
export function ContactStep({ contact, onChange, onNext, onBack }: ContactStepProps) {
  const { isAuthenticated, user } = useAuth()
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    if (!isAuthenticated || !user) {
      return
    }
    if (contact.fullName.length === 0 && contact.email === null) {
      onChange({ ...contact, fullName: user.fullName ?? contact.fullName, email: user.email })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, user])

  function handleNext() {
    const trimmedContact = { ...contact, fullName: contact.fullName.trim() }
    if (trimmedContact.fullName !== contact.fullName) {
      onChange(trimmedContact)
    }

    const validationErrors = validateContact(trimmedContact)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length === 0) {
      onNext()
    }
  }

  return (
    <div className="checkout-step">
      <h3>Dane kontaktowe</h3>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Imię i nazwisko
          <input value={contact.fullName} onChange={(e) => onChange({ ...contact, fullName: e.target.value })} />
          {errors.fullName && <span className="checkout-error">{errors.fullName}</span>}
        </label>

        <label className="checkout-field">
          Telefon
          <input
            placeholder="123456789"
            value={contact.phoneNumber}
            onChange={(e) => onChange({ ...contact, phoneNumber: e.target.value })}
          />
          {errors.phoneNumber && <span className="checkout-error">{errors.phoneNumber}</span>}
        </label>

        <label className="checkout-field">
          E-mail (opcjonalnie)
          <input
            value={contact.email ?? ''}
            onChange={(e) => onChange({ ...contact, email: e.target.value || null })}
          />
          {errors.email && <span className="checkout-error">{errors.email}</span>}
        </label>
      </div>

      <div className="checkout-actions">
        <button type="button" onClick={onBack}>
          Wstecz
        </button>
        <button type="button" className="add-to-cart-btn" onClick={handleNext}>
          Dalej
        </button>
      </div>
    </div>
  )
}
