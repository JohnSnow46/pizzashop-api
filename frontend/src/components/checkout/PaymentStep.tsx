import type { PaymentMethod } from '../../api/types'

interface PaymentStepProps {
  paymentMethod: PaymentMethod | null
  onSelect: (method: PaymentMethod) => void
  onNext: () => void
  onBack: () => void
}

/** Checkout step 5 (CLAUDE.md flow step 5): online (PayU) vs. on pickup/delivery. */
export function PaymentStep({ paymentMethod, onSelect, onNext, onBack }: PaymentStepProps) {
  return (
    <div className="checkout-step">
      <h3>Płatność</h3>

      <div className="checkout-choice-group">
        <button
          type="button"
          className={`checkout-choice${paymentMethod === 'Online' ? ' checkout-choice--selected' : ''}`}
          onClick={() => onSelect('Online')}
        >
          Online (PayU)
        </button>
        <button
          type="button"
          className={`checkout-choice${paymentMethod === 'OnPickup' ? ' checkout-choice--selected' : ''}`}
          onClick={() => onSelect('OnPickup')}
        >
          Przy odbiorze / dostawie
        </button>
      </div>

      <div className="checkout-actions">
        <button type="button" onClick={onBack}>
          Wstecz
        </button>
        <button type="button" className="add-to-cart-btn" disabled={paymentMethod === null} onClick={onNext}>
          Dalej
        </button>
      </div>
    </div>
  )
}
