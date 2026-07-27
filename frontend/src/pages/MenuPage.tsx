import { MenuList } from '../components/MenuList'
import { useMenu } from '../hooks/useMenu'

const SKELETON_CARD_COUNT = 6

/** Purely presentational placeholder shown while the menu loads (Faza 2 accent) — no data, no logic. */
function MenuSkeleton() {
  return (
    <div className="menu-skeleton-grid" role="status" aria-label="Ładowanie menu">
      {Array.from({ length: SKELETON_CARD_COUNT }).map((_, index) => (
        <div className="menu-skeleton-card" key={index}>
          <div className="menu-skeleton-block menu-skeleton-block--image" />
          <div className="menu-skeleton-block menu-skeleton-block--title" />
          <div className="menu-skeleton-block menu-skeleton-block--text" />
          <div className="menu-skeleton-block menu-skeleton-block--text-short" />
          <div className="menu-skeleton-block menu-skeleton-block--button" />
        </div>
      ))}
    </div>
  )
}

export function MenuPage() {
  const { items, loading, error } = useMenu()

  if (loading) {
    return <MenuSkeleton />
  }

  if (error) {
    return <p className="empty-state">Nie udało się załadować menu: {error}</p>
  }

  return <MenuList items={items} />
}
