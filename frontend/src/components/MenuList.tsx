import { useMemo, useState } from 'react'
import type { MenuCategory, MenuItem } from '../api/types'
import { CATEGORY_LABELS } from './menuCategoryLabels'
import { MenuItemCard } from './MenuItemCard'

interface MenuListProps {
  items: MenuItem[]
}

const CATEGORY_ORDER: MenuCategory[] = ['Pizza', 'Drink', 'Side', 'Dessert', 'Sauce']

export function MenuList({ items }: MenuListProps) {
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState<MenuCategory | 'all'>('all')
  const [onlyAvailable, setOnlyAvailable] = useState(false)

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
      if (onlyAvailable && !item.isAvailable) {
        return false
      }
      if (query && !item.name.toLowerCase().includes(query)) {
        return false
      }
      return true
    })
  }, [items, search, category, onlyAvailable])

  if (items.length === 0) {
    return <p className="empty-state">Menu jest obecnie puste.</p>
  }

  return (
    <div>
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

        <label className="menu-filters__checkbox">
          <input
            type="checkbox"
            checked={onlyAvailable}
            onChange={(e) => setOnlyAvailable(e.target.checked)}
          />
          Tylko dostępne
        </label>
      </div>

      {filteredItems.length === 0 ? (
        <p className="empty-state">Brak pozycji spełniających kryteria wyszukiwania.</p>
      ) : (
        <div className="menu-grid">
          {filteredItems.map((item) => (
            <MenuItemCard key={item.id} item={item} />
          ))}
        </div>
      )}
    </div>
  )
}
