import { useState } from 'react'
import type { ContactDetails } from '../../api/types'
import { validateContact } from '../../checkout/validation'

interface ContactStepProps {
  contact: ContactDetails
  onChange: (contact: ContactDetails) => void
  onNext: () => void
  onBack: () => void
}

/** Checkout step 3: guest contact details (CreateOrderCommand.Contact). */
export function ContactStep({ contact, onChange, onNext, onBack }: ContactStepProps) {
  const [errors, setErrors] = useState<Record<string, string>>({})

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
