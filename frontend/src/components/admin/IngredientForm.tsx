import { useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { createIngredient, updateIngredient } from '../../api/ingredientsApi'
import type { Ingredient } from '../../api/types'

interface IngredientFormProps {
  mode: 'create' | 'edit'
  /** Required when mode === 'edit'. */
  ingredient?: Ingredient
  onSaved: () => void
  onCancel: () => void
}

/** Add/edit form for a single Ingredient — shared between "Add ingredient" and its edit action. */
export function IngredientForm({ mode, ingredient, onSaved, onCancel }: IngredientFormProps) {
  const [name, setName] = useState(ingredient?.name ?? '')
  const [extraPriceAmount, setExtraPriceAmount] = useState(ingredient?.extraPrice.amount ?? 0)
  const [currency] = useState(ingredient?.extraPrice.currency ?? 'PLN')
  const [category, setCategory] = useState(ingredient?.category ?? '')
  const [isAvailable, setIsAvailable] = useState(ingredient?.isAvailable ?? true)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]> | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    setFieldErrors(null)

    try {
      if (mode === 'create') {
        await createIngredient({ name, extraPrice: { amount: extraPriceAmount, currency }, category: category || null })
      } else if (ingredient) {
        await updateIngredient(ingredient.id, { name, extraPrice: { amount: extraPriceAmount, currency }, isAvailable })
      }
      onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 400 && err.errors) {
        setFieldErrors(err.errors)
        setSubmitError(err.detail ?? err.title ?? 'Popraw błędy w formularzu.')
      } else if (err instanceof ApiError) {
        setSubmitError(err.detail ?? err.title ?? err.message)
      } else {
        setSubmitError('Nie udało się zapisać składnika.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="checkout-step admin-form" onSubmit={handleSubmit}>
      <h4>{mode === 'create' ? 'Nowy składnik' : `Edytuj: ${ingredient?.name}`}</h4>

      <div className="checkout-form-grid">
        <label className="checkout-field">
          Nazwa
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>

        <label className="checkout-field">
          Dopłata
          <input
            type="number"
            step="0.01"
            min="0"
            value={extraPriceAmount}
            onChange={(e) => setExtraPriceAmount(Number(e.target.value))}
          />
        </label>

        <label className="checkout-field">
          Kategoria (opcjonalnie)
          <input value={category} onChange={(e) => setCategory(e.target.value)} />
        </label>

        {mode === 'edit' && (
          <label className="checkout-field checkout-field--inline">
            <input type="checkbox" checked={isAvailable} onChange={(e) => setIsAvailable(e.target.checked)} />
            Dostępny
          </label>
        )}
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
