import type { FulfillmentType } from '../../api/types'
import type { CheckoutStep } from '../../checkout/checkoutState'

interface CheckoutStepperProps {
  step: CheckoutStep
  fulfillmentType: FulfillmentType | null
}

const LABELS: Record<CheckoutStep, string> = {
  1: 'Realizacja',
  2: 'Adres',
  3: 'Kontakt',
  4: 'Termin',
  5: 'Płatność',
  6: 'Kod promo',
  7: 'Podsumowanie',
}

/** Simple progress indicator — skips step 2 in the label list when fulfillment is Pickup. */
export function CheckoutStepper({ step, fulfillmentType }: CheckoutStepperProps) {
  const steps: CheckoutStep[] = fulfillmentType === 'Delivery' ? [1, 2, 3, 4, 5, 6, 7] : [1, 3, 4, 5, 6, 7]

  return (
    <ol className="checkout-stepper">
      {steps.map((s) => (
        <li key={s} className={s === step ? 'checkout-stepper__step--active' : ''}>
          {LABELS[s]}
        </li>
      ))}
    </ol>
  )
}
