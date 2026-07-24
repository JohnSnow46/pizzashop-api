import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { updateOpeningHours } from '../../api/restaurantApi'
import type { DayOfWeekName, OpeningHours, TimeRange } from '../../api/types'

const DAYS: { key: DayOfWeekName; label: string }[] = [
  { key: 'Monday', label: 'Poniedziałek' },
  { key: 'Tuesday', label: 'Wtorek' },
  { key: 'Wednesday', label: 'Środa' },
  { key: 'Thursday', label: 'Czwartek' },
  { key: 'Friday', label: 'Piątek' },
  { key: 'Saturday', label: 'Sobota' },
  { key: 'Sunday', label: 'Niedziela' },
]

interface DayState {
  open: boolean
  start: string
  end: string
}

/** Domain supports multiple ranges/day (e.g. a lunch break); this form edits a single
 * continuous range per day (or closed), which covers the restaurant's actual hours and
 * keeps the UI simple — a second range would need its own add/remove row UI. */
function toDayState(ranges: TimeRange[] | undefined): DayState {
  const first = ranges?.[0]
  return {
    open: !!first,
    start: first ? first.start.slice(0, 5) : '11:00',
    end: first ? first.end.slice(0, 5) : '22:00',
  }
}

interface OpeningHoursFormProps {
  openingHours: OpeningHours
  onSaved: () => void
}

/** Edit form for weekly opening hours (PUT /api/restaurant/opening-hours). */
export function OpeningHoursForm({ openingHours, onSaved }: OpeningHoursFormProps) {
  const [days, setDays] = useState<Record<DayOfWeekName, DayState>>(
    () =>
      Object.fromEntries(DAYS.map(({ key }) => [key, toDayState(openingHours.schedule[key])])) as Record<
        DayOfWeekName,
        DayState
      >,
  )

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  function updateDay(key: DayOfWeekName, patch: Partial<DayState>) {
    setDays((prev) => ({ ...prev, [key]: { ...prev[key], ...patch } }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    const schedule = Object.fromEntries(
      DAYS.map(({ key }) => {
        const day = days[key]
        return [key, day.open ? [{ start: `${day.start}:00`, end: `${day.end}:00` }] : []]
      }),
    ) as OpeningHours['schedule']

    try {
      await updateOpeningHours({ openingHours: { schedule } })
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać godzin otwarcia.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>Godziny otwarcia</h4>

      <div className="variant-editor">
        {DAYS.map(({ key, label }) => {
          const day = days[key]
          return (
            <div key={key} className="variant-row">
              <label className="checkout-field checkout-field--inline" style={{ minWidth: 130 }}>
                <input type="checkbox" checked={day.open} onChange={(e) => updateDay(key, { open: e.target.checked })} />
                {label}
              </label>
              <input
                type="time"
                value={day.start}
                disabled={!day.open}
                onChange={(e) => updateDay(key, { start: e.target.value })}
              />
              <span>–</span>
              <input
                type="time"
                value={day.end}
                disabled={!day.open}
                onChange={(e) => updateDay(key, { end: e.target.value })}
              />
            </div>
          )
        })}
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
      {submitError && <p className="checkout-error">{submitError}</p>}

      <div className="checkout-actions">
        <button type="submit" className="add-to-cart-btn" disabled={submitting}>
          {submitting ? 'Zapisywanie...' : 'Zapisz godziny otwarcia'}
        </button>
      </div>
    </form>
  )
}
