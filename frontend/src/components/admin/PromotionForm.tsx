import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { createPromotion, updatePromotion } from '../../api/promotionsApi'
import type { BuyXGetYRule, Promotion, PromotionType } from '../../api/types'

const PROMOTION_TYPES: PromotionType[] = ['Percentage', 'FixedAmount', 'FreeDelivery', 'BuyXGetY']

const TYPE_LABELS: Record<PromotionType, string> = {
  Percentage: 'Procentowa',
  FixedAmount: 'Kwota stała',
  FreeDelivery: 'Darmowa dostawa',
  BuyXGetY: 'Kup X dostań Y',
}

interface PromotionFormProps {
  mode: 'create' | 'edit'
  /** Required when mode === 'edit'. */
  promotion?: Promotion
  onSaved: () => void
  onCancel: () => void
}

/** ISO 8601 string (DateTimeOffset on the wire) -> value for an <input type="datetime-local">. */
function toDatetimeLocal(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

/** <input type="datetime-local"> value -> ISO 8601 string for the wire. */
function fromDatetimeLocal(value: string): string {
  return new Date(value).toISOString()
}

const emptyBuyXGetY: BuyXGetYRule = {
  triggerMenuItemId: '',
  buyQuantity: 1,
  rewardMenuItemId: '',
  getQuantity: 1,
  rewardDiscountPercentage: 100,
}

/**
 * Create/edit form for a Promotion. Create allows the full field set; edit is deliberately
 * narrower (ADR-0019) — only isActive, the valid-from/to window, value and usageLimit are
 * mutable. name/code/type/minOrderValue/buyXGetY are shown read-only (disabled) in edit mode so
 * the admin can still see what they're editing.
 */
export function PromotionForm({ mode, promotion, onSaved, onCancel }: PromotionFormProps) {
  const [name, setName] = useState(promotion?.name ?? '')
  const [code, setCode] = useState(promotion?.code ?? '')
  const [type, setType] = useState<PromotionType>(promotion?.type ?? 'Percentage')
  const [value, setValue] = useState<number | ''>(promotion?.value ?? '')
  const [minOrderValueAmount, setMinOrderValueAmount] = useState<number | ''>(promotion?.minOrderValue?.amount ?? '')
  const [validFrom, setValidFrom] = useState(
    promotion ? toDatetimeLocal(promotion.validFrom) : toDatetimeLocal(new Date().toISOString()),
  )
  const [validTo, setValidTo] = useState(
    promotion
      ? toDatetimeLocal(promotion.validTo)
      : toDatetimeLocal(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString()),
  )
  const [usageLimit, setUsageLimit] = useState<number | ''>(promotion?.usageLimit ?? '')
  const [isActive, setIsActive] = useState(promotion?.isActive ?? true)
  const [buyXGetY, setBuyXGetY] = useState<BuyXGetYRule>(promotion?.buyXGetY ?? emptyBuyXGetY)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  const needsValue = type === 'Percentage' || type === 'FixedAmount'
  const needsBuyXGetY = type === 'BuyXGetY'

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    try {
      if (mode === 'create') {
        await createPromotion({
          name,
          type,
          validFrom: fromDatetimeLocal(validFrom),
          validTo: fromDatetimeLocal(validTo),
          value: needsValue ? Number(value) : null,
          code: code || null,
          minOrderValue: minOrderValueAmount === '' ? null : { amount: Number(minOrderValueAmount), currency: 'PLN' },
          usageLimit: usageLimit === '' ? null : Number(usageLimit),
          buyXGetY: needsBuyXGetY ? buyXGetY : null,
        })
      } else if (promotion) {
        await updatePromotion(promotion.id, {
          isActive,
          validFrom: fromDatetimeLocal(validFrom),
          validTo: fromDatetimeLocal(validTo),
          value: needsValue ? (value === '' ? null : Number(value)) : null,
          usageLimit: usageLimit === '' ? null : Number(usageLimit),
        })
      }
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać promocji.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>{mode === 'create' ? 'Nowa promocja' : `Edytuj: ${promotion?.name}`}</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Nazwa
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={mode === 'edit'}
            required
          />
        </label>

        <label className="checkout-field">
          Kod (opcjonalnie)
          <input value={code} onChange={(e) => setCode(e.target.value)} disabled={mode === 'edit'} />
        </label>

        <label className="checkout-field">
          Typ
          <select
            value={type}
            onChange={(e) => setType(e.target.value as PromotionType)}
            disabled={mode === 'edit'}
          >
            {PROMOTION_TYPES.map((t) => (
              <option key={t} value={t}>
                {TYPE_LABELS[t]}
              </option>
            ))}
          </select>
          {mode === 'edit' && <small>Typ nie podlega edycji po utworzeniu (ADR-0019).</small>}
        </label>

        <label className="checkout-field">
          Minimalna wartość zamówienia (opcjonalnie)
          <input
            type="number"
            step="0.01"
            min="0"
            value={minOrderValueAmount}
            onChange={(e) => setMinOrderValueAmount(e.target.value === '' ? '' : Number(e.target.value))}
            disabled={mode === 'edit'}
          />
        </label>

        {needsValue && (
          <label className="checkout-field">
            Wartość {type === 'Percentage' ? '(%)' : '(PLN)'}
            <input
              type="number"
              step="0.01"
              min="0"
              max={type === 'Percentage' ? 100 : undefined}
              value={value}
              onChange={(e) => setValue(e.target.value === '' ? '' : Number(e.target.value))}
              required
            />
          </label>
        )}

        <label className="checkout-field">
          Ważna od
          <input type="datetime-local" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Ważna do
          <input type="datetime-local" value={validTo} onChange={(e) => setValidTo(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Limit użyć (opcjonalnie)
          <input
            type="number"
            min="1"
            value={usageLimit}
            onChange={(e) => setUsageLimit(e.target.value === '' ? '' : Number(e.target.value))}
          />
        </label>

        {mode === 'edit' && (
          <label className="checkout-field">
            Aktywna
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          </label>
        )}
      </div>

      {(needsBuyXGetY || (mode === 'edit' && promotion?.type === 'BuyXGetY')) && (
        <fieldset className="admin-fieldset">
          <legend>Konfiguracja Kup X dostań Y{mode === 'edit' ? ' (tylko do odczytu)' : ''}</legend>
          <div className="checkout-form-grid">
            <label className="checkout-field">
              ID pozycji wyzwalającej (GUID)
              <input
                value={buyXGetY.triggerMenuItemId}
                onChange={(e) => setBuyXGetY((r) => ({ ...r, triggerMenuItemId: e.target.value }))}
                disabled={mode === 'edit'}
                required={needsBuyXGetY}
              />
            </label>
            <label className="checkout-field">
              Ilość do kupienia
              <input
                type="number"
                min="1"
                value={buyXGetY.buyQuantity}
                onChange={(e) => setBuyXGetY((r) => ({ ...r, buyQuantity: Number(e.target.value) }))}
                disabled={mode === 'edit'}
              />
            </label>
            <label className="checkout-field">
              ID pozycji nagrody (GUID)
              <input
                value={buyXGetY.rewardMenuItemId}
                onChange={(e) => setBuyXGetY((r) => ({ ...r, rewardMenuItemId: e.target.value }))}
                disabled={mode === 'edit'}
                required={needsBuyXGetY}
              />
            </label>
            <label className="checkout-field">
              Ilość nagrody
              <input
                type="number"
                min="1"
                value={buyXGetY.getQuantity}
                onChange={(e) => setBuyXGetY((r) => ({ ...r, getQuantity: Number(e.target.value) }))}
                disabled={mode === 'edit'}
              />
            </label>
            <label className="checkout-field">
              Rabat na nagrodę (%)
              <input
                type="number"
                min="0"
                max="100"
                step="0.01"
                value={buyXGetY.rewardDiscountPercentage}
                onChange={(e) => setBuyXGetY((r) => ({ ...r, rewardDiscountPercentage: Number(e.target.value) }))}
                disabled={mode === 'edit'}
              />
            </label>
          </div>
        </fieldset>
      )}

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
        <button type="button" onClick={onCancel} disabled={submitting}>
          Anuluj
        </button>
        <button type="submit" className="add-to-cart-btn" disabled={submitting}>
          {submitting ? 'Zapisywanie...' : 'Zapisz'}
        </button>
      </div>
    </form>
  )
}
