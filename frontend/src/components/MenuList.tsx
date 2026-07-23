import type { MenuItem } from '../api/types'
import { MenuItemCard } from './MenuItemCard'

interface MenuListProps {
  items: MenuItem[]
}

export function MenuList({ items }: MenuListProps) {
  if (items.length === 0) {
    return <p className="empty-state">Menu jest obecnie puste.</p>
  }

  return (
    <div className="menu-grid">
      {items.map((item) => (
        <MenuItemCard key={item.id} item={item} />
      ))}
    </div>
  )
}
