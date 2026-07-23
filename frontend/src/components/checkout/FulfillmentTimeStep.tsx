import { useMemo, useState } from 'react'
import type { RestaurantConfig } from '../../api/types'
import { getWindowsForDate, isInPast, isWithinOpeningHours } from '../../checkout/openingHours'
import type { ScheduleState } from '../../checkout/checkoutState'

interface FulfillmentTimeStepProps {
  schedule: ScheduleState
  restaurantConfig: RestaurantConfig
  onChange: (schedule: ScheduleState) => void
  onNext: () => void
  onBack: () => void
}

function toDateInput(date: Date): string {
  return date.toISOString().slice(0, 10)
}

/**
 * Checkout step 4 (CLAUDE.md flow step 4): "now" vs. "scheduled". The scheduled picker is
 * limited to the restaurant's opening-hours windows as a UI convenience only — the
 * authoritative check (including TimeZoneId) is Domain's (ADR-0036, Decision point 6).
 */
export function FulfillmentTimeStep({ schedule, restaurantConfig, onChange, onNext, onBack }: FulfillmentTimeStepProps) {
  const [dateInput, setDateInput] = useState(() => toDateInput(new Date()))
  const [timeInput, setTimeInput] = useState('')
  const [error, setError] = useState<string | null>(null)

  const windows = useMemo(() => getWindowsForDate(restaurantConfig, new Date(dateInput)), [restaurantConfig, dateInput])

  function selectNow() {
    setError(null)
    onChange({ mode: 'now', at: null })
  }

  function selectScheduled() {
    setError(null)
    onChange({ mode: 'scheduled', at: schedule.at })
  }

  function applyScheduledTime(nextDate: string, nextTime: string) {
    setDateInput(nextDate)
    setTimeInput(nextTime)

    if (!nextTime) {
      onChange({ mode: 'scheduled', at: null })
      return
    }

    const when = new Date(`${nextDate}T${nextTime}`)
    onChange({ mode: 'scheduled', at: when.toISOString() })
  }

  function handleNext() {
    if (schedule.mode === 'now') {
      if (!restaurantConfig.isAcceptingOrders) {
        setError('Restauracja aktualnie nie przyjmuje zamówień "na teraz" — zaplanuj termin w godzinach pracy.')
        return
      }
      onNext()
      return
    }

    if (!schedule.at) {
      setError('Wybierz datę i godzinę.')
      return
    }

    const when = new Date(schedule.at)
    if (isInPast(when)) {
      setError('Wybrany termin już minął.')
      return
    }
    if (!isWithinOpeningHours(restaurantConfig, when)) {
      setError('Wybrany termin wypada poza godzinami pracy restauracji.')
      return
    }

    setError(null)
    onNext()
  }

  return (
    <div className="checkout-step">
      <h3>Termin realizacji</h3>

      <div className="checkout-choice-group">
        <button
          type="button"
          className={`checkout-choice${schedule.mode === 'now' ? ' checkout-choice--selected' : ''}`}
          onClick={selectNow}
        >
          Na teraz
        </button>
        <button
          type="button"
          className={`checkout-choice${schedule.mode === 'scheduled' ? ' checkout-choice--selected' : ''}`}
          onClick={selectScheduled}
        >
          Zaplanuj termin
        </button>
      </div>

      {schedule.mode === 'now' && !restaurantConfig.isAcceptingOrders && (
        <p className="checkout-banner checkout-banner--error">
          Restauracja aktualnie nie przyjmuje zamówień — możesz zaplanować termin w godzinach pracy.
        </p>
      )}

      {schedule.mode === 'scheduled' && (
        <div className="checkout-form-grid">
          <label className="checkout-field">
            Data
            <input
              type="date"
              value={dateInput}
              onChange={(e) => applyScheduledTime(e.target.value, timeInput)}
            />
          </label>
          <label className="checkout-field">
            Godzina
            <input type="time" value={timeInput} onChange={(e) => applyScheduledTime(dateInput, e.target.value)} />
          </label>

          <p className="checkout-hint">
            {windows.length > 0
              ? `Godziny otwarcia tego dnia: ${windows.map((w) => `${w.start}–${w.end}`).join(', ')}`
              : 'Restauracja jest zamknięta wybranego dnia.'}
          </p>
        </div>
      )}

      {error && <p className="checkout-banner checkout-banner--error">{error}</p>}

      <div className="checkout-actions">
        <button type="button" onClick={onBack}>
          Wstecz
        </button>
        <button type="button" className="add-to-cart-btn" onClick={handleNext}>
          Dalej
        </button>
      </div>
    </div>
  )
}
