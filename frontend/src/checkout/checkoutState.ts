import { useReducer } from 'react'
import type {
  Address,
  ContactDetails,
  DeliveryAvailability,
  FulfillmentType,
  PaymentMethod,
  PromotionDiscountPreview,
} from '../api/types'

/**
 * Wizard steps (ADR-0036, Decision point 1 / builder plan point 12):
 * 1 Fulfillment, 2 DeliveryAddress (Delivery only — skipped for Pickup), 3 Contact,
 * 4 FulfillmentTime, 5 Payment, 6 PromotionField, 7 OrderSummary (submit).
 */
export type CheckoutStep = 1 | 2 | 3 | 4 | 5 | 6 | 7

export interface ScheduleState {
  mode: 'now' | 'scheduled'
  /** ISO 8601 DateTimeOffset (with offset, not a bare local time) — only set when mode is 'scheduled'. */
  at: string | null
}

export interface CheckoutState {
  step: CheckoutStep
  fulfillmentType: FulfillmentType | null
  address: Address | null
  deliveryCheck: DeliveryAvailability | null
  contact: ContactDetails
  schedule: ScheduleState
  paymentMethod: PaymentMethod | null
  promotionCode: string | null
  promotionPreview: PromotionDiscountPreview | null
  /** Loyalty points the customer chose to redeem at checkout (ADR-0040), null for a guest/no selection. */
  pointsToRedeem: number | null
}

export const initialCheckoutState: CheckoutState = {
  step: 1,
  fulfillmentType: null,
  address: null,
  deliveryCheck: null,
  contact: { fullName: '', phoneNumber: '', email: null },
  schedule: { mode: 'now', at: null },
  paymentMethod: null,
  promotionCode: null,
  promotionPreview: null,
  pointsToRedeem: null,
}

export type CheckoutAction =
  | { type: 'setFulfillment'; fulfillmentType: FulfillmentType }
  | { type: 'setAddress'; address: Address }
  | { type: 'setDeliveryCheck'; result: DeliveryAvailability }
  | { type: 'setContact'; contact: ContactDetails }
  | { type: 'setSchedule'; schedule: ScheduleState }
  | { type: 'setPayment'; paymentMethod: PaymentMethod }
  | { type: 'setPromotion'; code: string | null; preview: PromotionDiscountPreview | null }
  | { type: 'setPointsToRedeem'; points: number | null }
  | { type: 'goNext' }
  | { type: 'goBack' }
  | { type: 'goToStep'; step: CheckoutStep }

/** Step 2 (delivery address) only applies when the fulfillment type is Delivery. */
function nextStep(step: CheckoutStep, fulfillmentType: FulfillmentType | null): CheckoutStep {
  if (step === 1) {
    return fulfillmentType === 'Delivery' ? 2 : 3
  }
  return Math.min(step + 1, 7) as CheckoutStep
}

function previousStep(step: CheckoutStep, fulfillmentType: FulfillmentType | null): CheckoutStep {
  if (step === 3) {
    return fulfillmentType === 'Delivery' ? 2 : 1
  }
  return Math.max(step - 1, 1) as CheckoutStep
}

function checkoutReducer(state: CheckoutState, action: CheckoutAction): CheckoutState {
  switch (action.type) {
    case 'setFulfillment':
      if (action.fulfillmentType === state.fulfillmentType) {
        return state
      }
      return {
        ...state,
        fulfillmentType: action.fulfillmentType,
        // Switching mode invalidates any previous delivery-address check.
        address: action.fulfillmentType === 'Delivery' ? state.address : null,
        deliveryCheck: action.fulfillmentType === 'Delivery' ? state.deliveryCheck : null,
      }
    case 'setAddress':
      return { ...state, address: action.address }
    case 'setDeliveryCheck':
      return { ...state, deliveryCheck: action.result }
    case 'setContact':
      return { ...state, contact: action.contact }
    case 'setSchedule':
      return { ...state, schedule: action.schedule }
    case 'setPayment':
      return { ...state, paymentMethod: action.paymentMethod }
    case 'setPromotion':
      return { ...state, promotionCode: action.code, promotionPreview: action.preview }
    case 'setPointsToRedeem':
      return { ...state, pointsToRedeem: action.points }
    case 'goNext':
      return { ...state, step: nextStep(state.step, state.fulfillmentType) }
    case 'goBack':
      return { ...state, step: previousStep(state.step, state.fulfillmentType) }
    case 'goToStep':
      return { ...state, step: action.step }
    default:
      return state
  }
}

export function useCheckoutState() {
  return useReducer(checkoutReducer, initialCheckoutState)
}
