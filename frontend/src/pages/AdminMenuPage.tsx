import { useState } from 'react'
import { IngredientsSection } from '../components/admin/IngredientsSection'
import { MenuItemForm } from '../components/admin/MenuItemForm'
import { MenuItemList } from '../components/admin/MenuItemList'
import type { MenuItem } from '../api/types'
import { useIngredients } from '../hooks/useIngredients'
import { useMenu } from '../hooks/useMenu'

type MenuFormState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; item: MenuItem }

/**
 * `/admin/menu` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) —
 * catalog management: menu items (list + availability toggle + create/edit form with variants
 * and ingredient selection, ADR-0016) and the ingredient dictionary (list + add/edit).
 */
export function AdminMenuPage() {
  const { items, loading, error, reload } = useMenu()
  const {
    ingredients,
    loading: ingredientsLoading,
    error: ingredientsError,
    reload: reloadIngredients,
  } = useIngredients()

  const [menuForm, setMenuForm] = useState<MenuFormState>({ mode: 'closed' })

  function handleMenuItemSaved() {
    setMenuForm({ mode: 'closed' })
    reload()
  }

  return (
    <div className="admin-page">
      <h2>Panel admina — katalog</h2>

      <section className="admin-section">
        <h3>Pozycje menu</h3>

        {loading && <p>Ładowanie menu...</p>}
        {error && <p className="empty-state">{error}</p>}

        {!loading && !error && (
          <MenuItemList
            items={items}
            onEdit={(item) => setMenuForm({ mode: 'edit', item })}
            onAvailabilityChanged={reload}
          />
        )}

        {menuForm.mode === 'closed' && (
          <button type="button" onClick={() => setMenuForm({ mode: 'create' })}>
            Dodaj nową pozycję
          </button>
        )}

        {menuForm.mode === 'create' && (
          <MenuItemForm
            mode="create"
            ingredients={ingredients}
            onSaved={handleMenuItemSaved}
            onCancel={() => setMenuForm({ mode: 'closed' })}
          />
        )}

        {menuForm.mode === 'edit' && (
          <MenuItemForm
            mode="edit"
            item={menuForm.item}
            ingredients={ingredients}
            onSaved={handleMenuItemSaved}
            onCancel={() => setMenuForm({ mode: 'closed' })}
          />
        )}
      </section>

      <IngredientsSection
        ingredients={ingredients}
        loading={ingredientsLoading}
        error={ingredientsError}
        onChanged={reloadIngredients}
      />
    </div>
  )
}
