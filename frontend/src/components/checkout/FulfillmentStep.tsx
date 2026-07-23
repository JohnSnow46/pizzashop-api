import type { FulfillmentType } from '../../api/types'

interface FulfillmentStepProps {
  fulfillmentType: FulfillmentType | null
  onSelect: (type: FulfillmentType) => void
  onNext: () => void
}

/** Checkout step 1 (CLAUDE.md flow step 1): delivery vs. pickup. */
export function FulfillmentStep({ fulfillmentType, onSelect, onNext }: FulfillmentStepProps) {
  return (
    <div className="checkout-step">
      <h3>Sposób realizacji</h3>
      <div className="checkout-choice-group">
        <button
          type="button"
          className={`checkout-choice${fulfillmentType === 'Delivery' ? ' checkout-choice--selected' : ''}`}
          onClick={() => onSelect('Delivery')}
        >
          Dostawa
        </button>
        <button
          type="button"
          className={`checkout-choice${fulfillmentType === 'Pickup' ? ' checkout-choice--selected' : ''}`}
          onClick={() => onSelect('Pickup')}
        >
          Odbiór osobisty
        </button>
      </div>

      <div className="checkout-actions">
        <button type="button" className="add-to-cart-btn" disabled={fulfillmentType === null} onClick={onNext}>
          Dalej
        </button>
      </div>
    </div>
  )
}
