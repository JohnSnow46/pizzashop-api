import { useState } from 'react'
import type { Ingredient } from '../../api/types'
import { IngredientForm } from './IngredientForm'

interface IngredientsSectionProps {
  ingredients: Ingredient[]
  loading: boolean
  error: string | null
  onChanged: () => void
}

type FormState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; ingredient: Ingredient }

/** Ingredient dictionary management: list with availability flag + an add/edit form. */
export function IngredientsSection({ ingredients, loading, error, onChanged }: IngredientsSectionProps) {
  const [form, setForm] = useState<FormState>({ mode: 'closed' })

  function handleSaved() {
    setForm({ mode: 'closed' })
    onChanged()
  }

  return (
    <section className="admin-section">
      <h3>Składniki</h3>

      {loading && <p>Ładowanie składników...</p>}
      {error && <p className="empty-state">{error}</p>}

      {!loading && !error && (
        ingredients.length > 0 ? (
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Nazwa</th>
                  <th>Kategoria</th>
                  <th>Dopłata</th>
                  <th>Dostępność</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {ingredients.map((ingredient) => (
                  <tr key={ingredient.id}>
                    <td>{ingredient.name}</td>
                    <td>{ingredient.category ?? '—'}</td>
                    <td>
                      {ingredient.extraPrice.amount.toFixed(2)} {ingredient.extraPrice.currency}
                    </td>
                    <td>{ingredient.isAvailable ? 'Dostępny' : 'Niedostępny'}</td>
                    <td>
                      <button type="button" onClick={() => setForm({ mode: 'edit', ingredient })}>
                        Edytuj
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="empty-state">Brak składników.</p>
        )
      )}

      {form.mode === 'closed' && (
        <button type="button" onClick={() => setForm({ mode: 'create' })}>
          Dodaj składnik
        </button>
      )}

      {form.mode === 'create' && (
        <IngredientForm mode="create" onSaved={handleSaved} onCancel={() => setForm({ mode: 'closed' })} />
      )}

      {form.mode === 'edit' && (
        <IngredientForm
          mode="edit"
          ingredient={form.ingredient}
          onSaved={handleSaved}
          onCancel={() => setForm({ mode: 'closed' })}
        />
      )}
    </section>
  )
}
