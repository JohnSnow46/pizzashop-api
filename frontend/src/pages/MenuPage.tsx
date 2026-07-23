import { MenuList } from '../components/MenuList'
import { useMenu } from '../hooks/useMenu'

export function MenuPage() {
  const { items, loading, error } = useMenu()

  if (loading) {
    return <p>Ładowanie menu...</p>
  }

  if (error) {
    return <p className="empty-state">Nie udało się załadować menu: {error}</p>
  }

  return <MenuList items={items} />
}
