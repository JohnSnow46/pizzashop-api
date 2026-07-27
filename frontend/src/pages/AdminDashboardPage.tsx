import { Link } from 'react-router-dom'

interface AdminDashboardTile {
  to: string
  label: string
  description: string
}

const TILES: AdminDashboardTile[] = [
  { to: '/admin/menu', label: 'Menu', description: 'Zarządzaj pozycjami menu i składnikami.' },
  { to: '/admin/promotions', label: 'Promocje', description: 'Twórz i edytuj promocje.' },
  { to: '/admin/restaurant', label: 'Restauracja', description: 'Godziny otwarcia, obszar dostawy, progi zamówień.' },
  { to: '/admin/staff', label: 'Pracownicy', description: 'Lista i zakładanie kont pracowniczych.' },
  { to: '/admin/reports', label: 'Raporty', description: 'Raporty sprzedaży i najlepiej sprzedające się pozycje.' },
]

/**
 * `/admin` (RequireAuth roles=RestaurantAdmin/SuperAdmin, mirrors AuthRoles.Admin) — landing
 * page for the admin panel with tile links into the existing admin sub-pages (menu,
 * promotions, restaurant, staff, reports).
 */
export function AdminDashboardPage() {
  return (
    <div className="admin-page">
      <h2>Panel admina</h2>

      <div className="admin-dashboard-grid">
        {TILES.map((tile) => (
          <Link key={tile.to} to={tile.to} className="admin-dashboard-tile">
            <h3>{tile.label}</h3>
            <p>{tile.description}</p>
          </Link>
        ))}
      </div>
    </div>
  )
}
