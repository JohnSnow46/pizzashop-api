import { useMemo, useState } from 'react'
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
  const [search, setSearch] = useState('')

  // Same name search MenuItemList already has next to it on /admin/menu, also matching the
  // free-text category -- the ingredient dictionary only grows, and finding "mozzarella" to
  // toggle its availability meant scrolling the whole table.
  const filteredIngredients = useMemo(() => {
    const query = search.trim().toLowerCase()
    if (!query) {
      return ingredients
    }
    return ingredients.filter(
      (ingredient) =>
        ingredient.name.toLowerCase().includes(query) || (ingredient.category ?? '').toLowerCase().includes(query),
    )
  }, [ingredients, search])

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
            <div className="menu-filters">
              <input
                type="search"
                className="menu-filters__search"
                placeholder="Szukaj składnika..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                aria-label="Szukaj składnika"
              />
            </div>
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
                {filteredIngredients.map((ingredient) => (
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
