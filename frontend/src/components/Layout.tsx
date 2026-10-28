import type { ReactNode } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import type { UserRole } from '../auth/types'
import { useAuth } from '../hooks/useAuth'
import { useCart } from '../hooks/useCart'

interface LayoutProps {
  children: ReactNode
}

/** Roles allowed onto /employee/orders — mirrors the RequireAuth roles in routes.tsx. */
const STAFF_ROLES: UserRole[] = ['Employee', 'RestaurantAdmin', 'SuperAdmin']

/** Roles allowed onto /admin, /admin/menu, /admin/promotions, /admin/restaurant, /admin/staff and /admin/reports — mirrors the RequireAuth roles in routes.tsx (AuthRoles.Admin). */
const ADMIN_ROLES: UserRole[] = ['RestaurantAdmin', 'SuperAdmin']

export function Layout({ children }: LayoutProps) {
  const { totalQuantity } = useCart()
  const { isAuthenticated, user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <div className="layout">
      <header className="header">
        <h1>
          <Link to="/">PizzaShop</Link>
        </h1>
        <div className="header-actions">
          {/* NavLink rather than Link so the page you're on is marked (aria-current + .active) --
              with up to eight links in the header, nothing said which one you'd clicked. "/admin"
              matches exactly, otherwise it would stay lit on every /admin/* page next to the
              more specific link. */}
          <NavLink to="/cart" className="cart-link">
            Koszyk ({totalQuantity})
          </NavLink>
          {isAuthenticated ? (
            <span className="auth-status">
              {user?.email}{' '}
              <NavLink to="/account" className="auth-account-link">
                Moje konto
              </NavLink>{' '}
              {user && STAFF_ROLES.includes(user.role) && (
                <>
                  <NavLink to="/employee/orders" className="auth-account-link">
                    Panel pracownika
                  </NavLink>{' '}
                </>
              )}
              {user && ADMIN_ROLES.includes(user.role) && (
                <>
                  <NavLink to="/admin" end className="auth-account-link">
                    Panel admina
                  </NavLink>{' '}
                  <NavLink to="/admin/promotions" className="auth-account-link">
                    Promocje
                  </NavLink>{' '}
                  <NavLink to="/admin/restaurant" className="auth-account-link">
                    Restauracja
                  </NavLink>{' '}
                  <NavLink to="/admin/staff" className="auth-account-link">
                    Pracownicy
                  </NavLink>{' '}
                  <NavLink to="/admin/reports" className="auth-account-link">
                    Raporty
                  </NavLink>{' '}
                </>
              )}
              <button type="button" className="auth-logout-btn" onClick={handleLogout}>
                Wyloguj
              </button>
            </span>
          ) : (
            <Link to="/login" className="auth-link">
              Zaloguj
            </Link>
          )}
        </div>
      </header>
      <main>{children}</main>
    </div>
  )
}
