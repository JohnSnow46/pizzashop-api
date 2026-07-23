import { useEffect, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { createOrder } from '../api/ordersApi'
import { getRestaurantConfig } from '../api/restaurantApi'
import type { CreateOrderCommand, RestaurantConfig } from '../api/types'
import { CheckoutStepper } from '../components/checkout/CheckoutStepper'
import { ContactStep } from '../components/checkout/ContactStep'
import { DeliveryAddressStep } from '../components/checkout/DeliveryAddressStep'
import { FulfillmentStep } from '../components/checkout/FulfillmentStep'
import { FulfillmentTimeStep } from '../components/checkout/FulfillmentTimeStep'
import { OrderSummary, type SubmitError } from '../components/checkout/OrderSummary'
import { PaymentStep } from '../components/checkout/PaymentStep'
import { PromotionField } from '../components/checkout/PromotionField'
import { useCheckoutState } from '../checkout/checkoutState'
import { cartItemsToOrderItems, cartItemsToPromotionLines } from '../checkout/mapCartToOrder'
import { saveOrderResult } from '../checkout/orderResultStorage'
import { useCart } from '../hooks/useCart'

/**
 * Guest checkout wizard (ADR-0036). A single component driving 7 steps via local state
 * (useCheckoutState) rather than one route per step — see the ADR for the rationale.
 */
export function CheckoutPage() {
  const { items, totalAmount, clear } = useCart()
  const navigate = useNavigate()
  const [state, dispatch] = useCheckoutState()

  const [config, setConfig] = useState<RestaurantConfig | null>(null)
  const [configLoading, setConfigLoading] = useState(true)
  const [configError, setConfigError] = useState<string | null>(null)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<SubmitError | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  useEffect(() => {
    let cancelled = false
    getRestaurantConfig()
      .then((result) => {
        if (!cancelled) setConfig(result)
      })
      .catch((err: unknown) => {
        if (!cancelled) setConfigError(err instanceof Error ? err.message : 'Nie udało się załadować danych restauracji.')
      })
      .finally(() => {
        if (!cancelled) setConfigLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  if (items.length === 0) {
    return <Navigate to="/cart" replace />
  }

  if (configLoading) {
    return <p>Ładowanie...</p>
  }

  if (configError || !config) {
    return <p className="empty-state">Nie udało się załadować danych restauracji: {configError}</p>
  }

  const currency = items[0]?.currency ?? config.deliveryFee.currency

  async function handleSubmit() {
    if (!state.fulfillmentType || !state.paymentMethod) {
      return
    }

    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    const command: CreateOrderCommand = {
      contact: state.contact,
      fulfillmentType: state.fulfillmentType,
      deliveryAddress: state.fulfillmentType === 'Delivery' ? state.address : null,
      items: cartItemsToOrderItems(items),
      requestedFulfillmentTime: state.schedule.mode === 'scheduled' ? state.schedule.at : null,
      paymentMethod: state.paymentMethod,
      promotionCode: state.promotionCode,
      pointsToRedeem: null,
    }

    try {
      const result = await createOrder(command)
      saveOrderResult(result)
      clear()

      if (state.paymentMethod === 'Online' && result.paymentRedirectUrl) {
        window.location.href = result.paymentRedirectUrl
      } else {
        navigate('/checkout/confirmation')
      }
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400 && err.errors) {
          setFieldErrors(err.errors)
          setSubmitError({ message: err.detail ?? err.title ?? 'Popraw błędy w formularzu.' })
        } else if (err.status === 422 || err.status === 409) {
          setSubmitError({
            message: err.detail ?? err.title ?? 'Zamówienie nie mogło zostać złożone.',
            showSwitchToPickup: err.status === 422 && state.fulfillmentType === 'Delivery',
          })
        } else {
          setSubmitError({ message: err.detail ?? err.title ?? err.message })
        }
      } else {
        setSubmitError({ message: 'Nie udało się złożyć zamówienia. Spróbuj ponownie.' })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="checkout-page">
      <h2>Zamówienie</h2>
      <CheckoutStepper step={state.step} fulfillmentType={state.fulfillmentType} />

      {state.step === 1 && (
        <FulfillmentStep
          fulfillmentType={state.fulfillmentType}
          onSelect={(type) => dispatch({ type: 'setFulfillment', fulfillmentType: type })}
          onNext={() => dispatch({ type: 'goNext' })}
        />
      )}

      {state.step === 2 && (
        <DeliveryAddressStep
          address={state.address}
          deliveryCheck={state.deliveryCheck}
          onChecked={(address, result) => {
            dispatch({ type: 'setAddress', address })
            dispatch({ type: 'setDeliveryCheck', result })
          }}
          onSwitchToPickup={() => {
            dispatch({ type: 'setFulfillment', fulfillmentType: 'Pickup' })
            dispatch({ type: 'goToStep', step: 1 })
          }}
          onNext={() => dispatch({ type: 'goNext' })}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}

      {state.step === 3 && (
        <ContactStep
          contact={state.contact}
          onChange={(contact) => dispatch({ type: 'setContact', contact })}
          onNext={() => dispatch({ type: 'goNext' })}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}

      {state.step === 4 && (
        <FulfillmentTimeStep
          schedule={state.schedule}
          restaurantConfig={config}
          onChange={(schedule) => dispatch({ type: 'setSchedule', schedule })}
          onNext={() => dispatch({ type: 'goNext' })}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}

      {state.step === 5 && (
        <PaymentStep
          paymentMethod={state.paymentMethod}
          onSelect={(method) => dispatch({ type: 'setPayment', paymentMethod: method })}
          onNext={() => dispatch({ type: 'goNext' })}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}

      {state.step === 6 && (
        <PromotionField
          code={state.promotionCode}
          preview={state.promotionPreview}
          subtotal={{ amount: totalAmount, currency }}
          deliveryFee={
            state.fulfillmentType === 'Delivery' && state.deliveryCheck?.deliveryFee
              ? state.deliveryCheck.deliveryFee
              : { amount: 0, currency }
          }
          lines={cartItemsToPromotionLines(items)}
          onApply={(code, preview) => dispatch({ type: 'setPromotion', code, preview })}
          onNext={() => dispatch({ type: 'goNext' })}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}

      {state.step === 7 && (
        <OrderSummary
          items={items}
          subtotalAmount={totalAmount}
          currency={currency}
          state={state}
          restaurantConfig={config}
          submitting={submitting}
          submitError={submitError}
          fieldErrors={fieldErrors}
          onSwitchToPickup={() => dispatch({ type: 'setFulfillment', fulfillmentType: 'Pickup' })}
          onSubmit={handleSubmit}
          onBack={() => dispatch({ type: 'goBack' })}
        />
      )}
    </div>
  )
}
