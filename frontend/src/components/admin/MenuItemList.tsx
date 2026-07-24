import { useState } from 'react'
import { setMenuItemAvailability } from '../../api/menuApi'
import type { MenuItem } from '../../api/types'

interface MenuItemListProps {
  items: MenuItem[]
  onEdit: (item: MenuItem) => void
  /** Called after a successful availability toggle so the caller can refetch the list. */
  onAvailabilityChanged: () => void
}

/** Read-only table of all menu items with an availability toggle and an edit action. */
export function MenuItemList({ items, onEdit, onAvailabilityChanged }: MenuItemListProps) {
  const [pendingId, setPendingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

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
      <table className="admin-table">
        <thead>
          <tr>
            <th>Nazwa</th>
            <th>Kategoria</th>
            <th>Cena bazowa</th>
            <th>Dostępność</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.name}</td>
              <td>{item.category}</td>
              <td>
                {item.basePrice.amount.toFixed(2)} {item.basePrice.currency}
              </td>
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
    </div>
  )
}
