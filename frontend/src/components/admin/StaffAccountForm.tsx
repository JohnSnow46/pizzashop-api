import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { createStaffAccount } from '../../api/staffApi'
import type { UserRole } from '../../auth/types'

/**
 * Roles this form may request. Never SuperAdmin/Customer (out of scope for this UI) — the
 * backend handler still enforces "RestaurantAdmin can only create Employee" (ADR-0017), so a
 * RestaurantAdmin caller picking RestaurantAdmin here surfaces as a generic API error below.
 */
const CREATABLE_ROLES: UserRole[] = ['Employee', 'RestaurantAdmin']

const ROLE_LABELS: Record<UserRole, string> = {
  Customer: 'Klient',
  Employee: 'Pracownik',
  RestaurantAdmin: 'Administrator restauracji',
  SuperAdmin: 'Super administrator',
}

interface StaffAccountFormProps {
  onCreated: () => void
  onCancel: () => void
}

/** Create-only form for a staff account (list + create, no edit — admin staff UI). */
export function StaffAccountForm({ onCreated, onCancel }: StaffAccountFormProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<UserRole>('Employee')

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    try {
      await createStaffAccount({ email, password, role })
      onCreated()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się utworzyć konta pracownika.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>Nowe konto pracownika</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Hasło
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Rola
          <select value={role} onChange={(e) => setRole(e.target.value as UserRole)}>
            {CREATABLE_ROLES.map((r) => (
              <option key={r} value={r}>
                {ROLE_LABELS[r]}
              </option>
            ))}
          </select>
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
        <button type="button" onClick={onCancel} disabled={submitting}>
          Anuluj
        </button>
        <button type="submit" className="add-to-cart-btn" disabled={submitting}>
          {submitting ? 'Zapisywanie...' : 'Zapisz'}
        </button>
      </div>
    </form>
  )
}
