import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { validateEmail, validateLoginPassword } from '../auth/validation'
import { useAuth } from '../hooks/useAuth'

/** /login (ADR-0037) — email/password, auto-redirects to "/" on success. */
export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [banner, setBanner] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setBanner(null)

    const validationErrors: Record<string, string> = {}
    const emailError = validateEmail(email)
    if (emailError) validationErrors.email = emailError
    const passwordError = validateLoginPassword(password)
    if (passwordError) validationErrors.password = passwordError
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setSubmitting(true)
    try {
      await login(email.trim(), password)
      navigate('/')
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setBanner('Nieprawidłowy e-mail lub hasło.')
      } else if (err instanceof ApiError) {
        setBanner(err.detail ?? err.title ?? 'Nie udało się zalogować. Spróbuj ponownie.')
      } else {
        setBanner('Nie udało się zalogować. Spróbuj ponownie.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="checkout-page">
      <h2>Zaloguj się</h2>

      {banner && <div className="checkout-banner checkout-banner--error">{banner}</div>}

      <form className="checkout-step" onSubmit={handleSubmit}>
        <div className="checkout-form-grid">
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
              autoComplete="current-password"
            />
            {errors.password && <span className="checkout-error">{errors.password}</span>}
          </label>
        </div>

        <div className="checkout-actions">
          <button type="submit" className="add-to-cart-btn" disabled={submitting}>
            {submitting ? 'Logowanie...' : 'Zaloguj się'}
          </button>
        </div>
      </form>

      <p className="checkout-hint">
        Nie masz konta? <Link to="/register">Zarejestruj się</Link>
      </p>
    </div>
  )
}
