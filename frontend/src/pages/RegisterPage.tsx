import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { validateEmail, validateFullName, validateNewPassword, validatePhoneNumberOptional } from '../auth/validation'
import { useAuth } from '../hooks/useAuth'

/** /register (ADR-0037) — email/password/fullName/phone (optional), auto-login on success. */
export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [banner, setBanner] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setBanner(null)

    const validationErrors: Record<string, string> = {}
    const emailError = validateEmail(email)
    if (emailError) validationErrors.email = emailError
    const passwordError = validateNewPassword(password)
    if (passwordError) validationErrors.password = passwordError
    const fullNameError = validateFullName(fullName)
    if (fullNameError) validationErrors.fullName = fullNameError
    const phoneError = validatePhoneNumberOptional(phoneNumber)
    if (phoneError) validationErrors.phoneNumber = phoneError
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setSubmitting(true)
    try {
      const trimmedPhone = phoneNumber.trim()
      await register(email.trim(), password, fullName.trim(), trimmedPhone.length > 0 ? trimmedPhone : null)
      navigate('/')
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setBanner('Konto z tym adresem e-mail już istnieje.')
      } else if (err instanceof ApiError) {
        setBanner(err.detail ?? err.title ?? 'Nie udało się utworzyć konta. Spróbuj ponownie.')
      } else {
        setBanner('Nie udało się utworzyć konta. Spróbuj ponownie.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="checkout-page">
      <h2>Załóż konto</h2>

      {banner && <div className="checkout-banner checkout-banner--error">{banner}</div>}

      <form className="checkout-step" onSubmit={handleSubmit}>
        <div className="checkout-form-grid">
          <label className="checkout-field">
            Imię i nazwisko
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" />
            {errors.fullName && <span className="checkout-error">{errors.fullName}</span>}
          </label>

          <label className="checkout-field">
            E-mail
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
            {errors.email && <span className="checkout-error">{errors.email}</span>}
          </label>

          <label className="checkout-field">
            Hasło
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="new-password"
            />
            {errors.password && <span className="checkout-error">{errors.password}</span>}
          </label>

          <label className="checkout-field">
            Telefon (opcjonalnie)
            <input value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} autoComplete="tel" />
            {errors.phoneNumber && <span className="checkout-error">{errors.phoneNumber}</span>}
          </label>
        </div>

        <div className="checkout-actions">
          <button type="submit" className="add-to-cart-btn" disabled={submitting}>
            {submitting ? 'Tworzenie konta...' : 'Załóż konto'}
          </button>
        </div>
      </form>

      <p className="checkout-hint">
        Masz już konto? <Link to="/login">Zaloguj się</Link>
      </p>
    </div>
  )
}
