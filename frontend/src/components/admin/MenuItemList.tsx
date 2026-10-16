import { useMemo, useState } from 'react'
import { setMenuItemAvailability } from '../../api/menuApi'
import type { MenuCategory, MenuItem } from '../../api/types'
import { CATEGORY_LABELS } from '../menuCategoryLabels'

interface MenuItemListProps {
  items: MenuItem[]
  onEdit: (item: MenuItem) => void
  /** Called after a successful availability toggle so the caller can refetch the list. */
  onAvailabilityChanged: () => void
}

const CATEGORY_ORDER: MenuCategory[] = ['Pizza', 'Drink', 'Side', 'Dessert', 'Sauce']

/** Read-only table of all menu items with a name/category filter, an availability toggle,
 * and an edit action. The customer-facing MenuList (MenuPage) already has this same
 * search+category filter -- useful there at a handful of items, more useful here once
 * the catalog has more than a screenful of rows to manage. */
export function MenuItemList({ items, onEdit, onAvailabilityChanged }: MenuItemListProps) {
  const [pendingId, setPendingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState<MenuCategory | 'all'>('all')

  const availableCategories = useMemo(
    () => CATEGORY_ORDER.filter((c) => items.some((item) => item.category === c)),
    [items],
  )

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase()
    return items.filter((item) => {
      if (category !== 'all' && item.category !== category) {
        return false
      }
      if (query && !item.name.toLowerCase().includes(query)) {
        return false
      }
      return true
    })
  }, [items, search, category])

  async function toggleAvailability(item: MenuItem) {
    setPendingId(item.id)
    setError(null)
    try {
      await setMenuItemAvailability(item.id, !item.isAvailable)
      onAvailabilityChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Nie udało się zmienić dostępności.')
    } finally {
      setPendingId(null)
    }
  }

  if (items.length === 0) {
    return <p className="empty-state">Brak pozycji w menu.</p>
  }

  return (
    <div className="admin-table-wrap">
      {error && <p className="checkout-error">{error}</p>}

      <div className="menu-filters">
        <input
          type="search"
          className="menu-filters__search"
          placeholder="Szukaj w menu..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Szukaj w menu"
        />

        <select
          value={category}
          onChange={(e) => setCategory(e.target.value as MenuCategory | 'all')}
          aria-label="Filtruj po kategorii"
        >
          <option value="all">Wszystkie kategorie</option>
          {availableCategories.map((c) => (
            <option key={c} value={c}>
              {CATEGORY_LABELS[c]}
            </option>
          ))}
        </select>
      </div>

      {filteredItems.length === 0 ? (
        <p className="empty-state">Brak pozycji spełniających kryteria wyszukiwania.</p>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Nazwa</th>
              <th>Kategoria</th>
              <th>Cena bazowa</th>
              <th>Ocena</th>
              <th>Dostępność</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {filteredItems.map((item) => (
              <tr key={item.id}>
                <td>{item.name}</td>
                <td>{CATEGORY_LABELS[item.category]}</td>
                <td>
                  {item.basePrice.amount.toFixed(2)} {item.basePrice.currency}
                </td>
                <td>{item.averageRating !== null ? `★ ${item.averageRating.toFixed(1)} (${item.reviewCount})` : '—'}</td>
                <td>
                  <button
                    type="button"
                    className={`admin-toggle${item.isAvailable ? ' admin-toggle--on' : ''}`}
                    disabled={pendingId === item.id}
                    onClick={() => void toggleAvailability(item)}
                  >
                    {item.isAvailable ? 'Dostępna' : 'Niedostępna'}
                  </button>
                </td>
                <td>
                  <button type="button" onClick={() => onEdit(item)}>
                    Edytuj
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
