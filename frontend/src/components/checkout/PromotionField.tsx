import { useState } from 'react'
import { ApiError } from '../../api/client'
import { validatePromotion } from '../../api/promotionsApi'
import type { Money, PromotionDiscountLine, PromotionDiscountPreview } from '../../api/types'

interface PromotionFieldProps {
  code: string | null
  preview: PromotionDiscountPreview | null
  subtotal: Money
  deliveryFee: Money
  lines: PromotionDiscountLine[]
  onApply: (code: string | null, preview: PromotionDiscountPreview | null) => void
  onNext: () => void
  onBack: () => void
}

/**
 * Checkout step 6: optional coupon code. Only a preview (ADR-0036) — the real discount is
 * (re)computed server-side when the order is created.
 */
export function PromotionField({ code, preview, subtotal, deliveryFee, lines, onApply, onNext, onBack }: PromotionFieldProps) {
  const [input, setInput] = useState(code ?? '')
  const [checking, setChecking] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Code typed but not yet checked (or checked and cleared since) — force the user to verify
  // it before moving on, otherwise it would silently be dropped from the order.
  const hasUncheckedCode = input.trim().length > 0 && preview === null

  async function handleCheck() {
    if (input.trim().length === 0) {
      onApply(null, null)
      return
    }

    setChecking(true)
    setError(null)
    try {
      const result = await validatePromotion({ code: input.trim(), subtotal, deliveryFee, lines })
      onApply(input.trim(), result)
    } catch (err) {
      setError(err instanceof ApiError ? (err.detail ?? err.message) : 'Nie udało się sprawdzić kodu promocyjnego.')
      onApply(input.trim(), null)
    } finally {
      setChecking(false)
    }
  }

  return (
    <div className="checkout-step">
      <h3>Kod promocyjny</h3>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Kod (opcjonalnie)
          <input value={input} onChange={(e) => setInput(e.target.value)} placeholder="np. PIZZA10" />
        </label>
      </div>

      <div className="checkout-actions">
        <button type="button" disabled={checking} onClick={handleCheck}>
          {checking ? 'Sprawdzanie...' : 'Sprawdź kod'}
        </button>
      </div>

      {error && <p className="checkout-banner checkout-banner--error">{error}</p>}

      {hasUncheckedCode && (
        <p className="checkout-hint">Sprawdź kod przed przejściem dalej albo wyczyść pole, żeby pominąć promocję.</p>
      )}

      {preview && (
        <p className={`checkout-banner ${preview.isQualified ? 'checkout-banner--success' : 'checkout-banner--error'}`}>
          {preview.isQualified && preview.discountAmount
            ? `Kod zastosowany — rabat orientacyjny: ${preview.discountAmount.amount.toFixed(2)} ${preview.discountAmount.currency}.`
            : 'Kod nie kwalifikuje się do rabatu dla tego zamówienia.'}
        </p>
      )}

      <div className="checkout-actions">
        <button type="button" onClick={onBack}>
          Wstecz
        </button>
        <button type="button" className="add-to-cart-btn" disabled={hasUncheckedCode} onClick={onNext}>
          Dalej
        </button>
      </div>
    </div>
  )
}
