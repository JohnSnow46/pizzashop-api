import type { RestaurantConfig } from '../../api/types'
import type { CheckoutState } from '../../checkout/checkoutState'
import type { CartItem } from '../../cart/types'
import { LoyaltyPointsField } from './LoyaltyPointsField'

export interface SubmitError {
  message: string
  showSwitchToPickup?: boolean
}

interface OrderSummaryProps {
  items: CartItem[]
  subtotalAmount: number
  currency: string
  state: CheckoutState
  restaurantConfig: RestaurantConfig
  submitting: boolean
  submitError: SubmitError | null
  fieldErrors: Record<string, string[]> | null
  onSwitchToPickup: () => void
  onPointsToRedeemChange: (points: number | null) => void
  onSubmit: () => void
  onBack: () => void
}

/** Checkout step 7: read-only recap of everything chosen so far, plus the final submit. */
export function OrderSummary({
  items,
  subtotalAmount,
  currency,
  state,
  restaurantConfig,
  submitting,
  submitError,
  fieldErrors,
  onSwitchToPickup,
  onPointsToRedeemChange,
  onSubmit,
  onBack,
}: OrderSummaryProps) {
  const deliveryFeeAmount = state.fulfillmentType === 'Delivery' ? (state.deliveryCheck?.deliveryFee?.amount ?? 0) : 0
  const discountAmount = state.promotionPreview?.isQualified ? (state.promotionPreview.discountAmount?.amount ?? 0) : 0
  // 0.05 PLN/point mirrors LinearLoyaltyPolicy (ADR-0033/ADR-0040, see LoyaltyPointsField) —
  // only for this orientational total, the server recalculates authoritatively.
  const pointsDiscountAmount = (state.pointsToRedeem ?? 0) * 0.05
  const total = subtotalAmount + deliveryFeeAmount - discountAmount - pointsDiscountAmount

  const minimumOrderValue = restaurantConfig.minimumOrderValue
  const belowMinimum = minimumOrderValue !== null && subtotalAmount < minimumOrderValue.amount

  const freeDeliveryThreshold = restaurantConfig.freeDeliveryThreshold
  const showFreeDeliveryHint =
    state.fulfillmentType === 'Delivery' && freeDeliveryThreshold !== null && subtotalAmount < freeDeliveryThreshold.amount

  return (
    <div className="checkout-step">
      <h3>Podsumowanie</h3>

      <ul className="checkout-summary-list">
        {items.map((item) => (
          <li key={item.key}>
            {item.quantity}× {item.menuItemName}
            {item.variantName ? ` — ${item.variantName}` : ''}
            {item.extraNames.length > 0 ? ` (${item.extraNames.join(', ')})` : ''}
          </li>
        ))}
      </ul>

      <div className="cart-summary">
        <span>Suma pozycji (orientacyjnie)</span>
        <span>
          {subtotalAmount.toFixed(2)} {currency}
        </span>
      </div>

      {state.fulfillmentType === 'Delivery' && (
        <div className="cart-summary">
          <span>Dostawa</span>
          <span>
            {deliveryFeeAmount.toFixed(2)} {currency}
          </span>
        </div>
      )}

      {discountAmount > 0 && (
        <div className="cart-summary">
          <span>Rabat (orientacyjnie)</span>
          <span>
            -{discountAmount.toFixed(2)} {currency}
          </span>
        </div>
      )}

      {pointsDiscountAmount > 0 && (
        <div className="cart-summary">
          <span>Punkty lojalnościowe (orientacyjnie)</span>
          <span>
            -{pointsDiscountAmount.toFixed(2)} {currency}
          </span>
        </div>
      )}

      <div className="cart-summary">
        <strong>Razem (orientacyjnie)</strong>
        <strong>
          {total.toFixed(2)} {currency}
        </strong>
      </div>

      <LoyaltyPointsField
        subtotal={{ amount: subtotalAmount, currency }}
        promoDiscount={{ amount: discountAmount, currency }}
        deliveryFee={{ amount: deliveryFeeAmount, currency }}
        pointsToRedeem={state.pointsToRedeem}
        onChange={onPointsToRedeemChange}
      />

      {showFreeDeliveryHint && freeDeliveryThreshold && (
        <p className="checkout-hint">
          Darmowa dostawa od {freeDeliveryThreshold.amount.toFixed(2)} {freeDeliveryThreshold.currency}.
        </p>
      )}

      {belowMinimum && minimumOrderValue && (
        <p className="checkout-banner checkout-banner--error">
          Minimalna wartość zamówienia to {minimumOrderValue.amount.toFixed(2)} {minimumOrderValue.currency}.
        </p>
      )}

      <div className="checkout-summary-details">
        <p>
          <strong>Kontakt:</strong> {state.contact.fullName}, {state.contact.phoneNumber}
          {state.contact.email ? `, ${state.contact.email}` : ''}
        </p>
        {state.fulfillmentType === 'Delivery' && state.address && (
          <p>
            <strong>Adres:</strong> {state.address.street} {state.address.buildingNumber}
            {state.address.apartmentNumber ? `/${state.address.apartmentNumber}` : ''}, {state.address.postalCode}{' '}
            {state.address.city}
          </p>
        )}
        <p>
          <strong>Termin:</strong>{' '}
          {state.schedule.mode === 'now' ? 'Na teraz' : new Date(state.schedule.at ?? '').toLocaleString('pl-PL')}
        </p>
        <p>
          <strong>Płatność:</strong> {state.paymentMethod === 'Online' ? 'Online (PayU)' : 'Przy odbiorze/dostawie'}
        </p>
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

      {submitError && (
        <div className="checkout-banner checkout-banner--error">
          <p>{submitError.message}</p>
          {submitError.showSwitchToPickup && (
            <button type="button" onClick={onSwitchToPickup}>
              Wybierz odbiór osobisty
            </button>
          )}
        </div>
      )}

      <div className="checkout-actions">
        <button type="button" onClick={onBack} disabled={submitting}>
          Wstecz
        </button>
        <button type="button" className="add-to-cart-btn" disabled={submitting || belowMinimum} onClick={onSubmit}>
          {submitting ? 'Składanie zamówienia...' : 'Zamawiam'}
        </button>
      </div>
    </div>
  )
}
