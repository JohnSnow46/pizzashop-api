import { useEffect, useState } from 'react'
import { getLoyaltyBalance } from '../../api/loyaltyApi'
import { useAuth } from '../../hooks/useAuth'
import type { Money } from '../../api/types'

/**
 * Mirrors PizzaShop.Infrastructure.Loyalty.LinearLoyaltyPolicy.RedemptionValuePerPoint
 * (ADR-0033/ADR-0040): 1 point = 0.05 PLN when redeemed. Used here only for a UX preview —
 * the server recalculates and enforces the real discount authoritatively.
 */
const REDEMPTION_VALUE_PER_POINT = 0.05

interface LoyaltyPointsFieldProps {
  subtotal: Money
  promoDiscount: Money
  deliveryFee: Money
  pointsToRedeem: number | null
  onChange: (points: number | null) => void
}

/**
 * Checkout step 7 addon (ADR-0040): lets a logged-in customer redeem loyalty points against
 * this order. Hidden entirely for a guest or an employee/admin session, and when the balance
 * is zero. The slider's max is only a UX guard (`min(balance, maxByOrderValue)`) — the server
 * (`Order.RedeemLoyaltyPoints`/`LoyaltyAccount.Redeem`) is the actual source of truth.
 */
export function LoyaltyPointsField({ subtotal, promoDiscount, deliveryFee, pointsToRedeem, onChange }: LoyaltyPointsFieldProps) {
  const { isAuthenticated, user } = useAuth()
  const [balance, setBalance] = useState<number | null>(null)
  const [loading, setLoading] = useState(false)

  const isEligible = isAuthenticated && user?.role === 'Customer'

  useEffect(() => {
    if (!isEligible) {
      return
    }

    let cancelled = false
    setLoading(true)
    getLoyaltyBalance()
      .then((result) => {
        if (!cancelled) setBalance(result.pointsBalance)
      })
      .catch(() => {
        if (!cancelled) setBalance(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [isEligible])

  if (!isEligible || loading || !balance || balance <= 0) {
    return null
  }

  const remainingPayable = subtotal.amount - promoDiscount.amount + deliveryFee.amount
  const maxByOrderValue = Math.max(0, Math.floor(remainingPayable / REDEMPTION_VALUE_PER_POINT))
  const maxPoints = Math.min(balance, maxByOrderValue)

  if (maxPoints <= 0) {
    return null
  }

  const points = Math.min(pointsToRedeem ?? 0, maxPoints)
  const discountPreview = points * REDEMPTION_VALUE_PER_POINT

  function handleChange(value: number) {
    const clamped = Math.max(0, Math.min(value, maxPoints))
    onChange(clamped > 0 ? clamped : null)
  }

  return (
    <div className="checkout-step loyalty-points-field">
      <h4>Wykorzystaj punkty lojalnościowe</h4>
      <p className="checkout-hint">
        Dostępne punkty: {balance} (1 pkt = {REDEMPTION_VALUE_PER_POINT.toFixed(2)} {subtotal.currency})
      </p>

      <label className="checkout-field">
        Punkty do wykorzystania (max {maxPoints})
        <input
          type="range"
          min={0}
          max={maxPoints}
          step={1}
          value={points}
          onChange={(e) => handleChange(Number(e.target.value))}
        />
        <input
          type="number"
          min={0}
          max={maxPoints}
          step={1}
          value={points}
          onChange={(e) => handleChange(Number(e.target.value))}
        />
      </label>

      {points > 0 && (
        <p className="checkout-banner checkout-banner--success">
          Rabat orientacyjny: -{discountPreview.toFixed(2)} {subtotal.currency}
        </p>
      )}
    </div>
  )
}
