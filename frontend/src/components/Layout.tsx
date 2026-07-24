import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import type { UserRole } from '../auth/types'
import { useAuth } from '../hooks/useAuth'
import { useCart } from '../hooks/useCart'

interface LayoutProps {
  children: ReactNode
}

/** Roles allowed onto /employee/orders — mirrors the RequireAuth roles in routes.tsx. */
const STAFF_ROLES: UserRole[] = ['Employee', 'RestaurantAdmin', 'SuperAdmin']

/** Roles allowed onto /admin/menu — mirrors the RequireAuth roles in routes.tsx (AuthRoles.Admin). */
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
          <Link to="/cart" className="cart-link">
            Koszyk ({totalQuantity})
          </Link>
          {isAuthenticated ? (
            <span className="auth-status">
              {user?.email}{' '}
              <Link to="/account" className="auth-account-link">
                Moje konto
              </Link>{' '}
              {user && STAFF_ROLES.includes(user.role) && (
                <>
                  <Link to="/employee/orders" className="auth-account-link">
                    Panel pracownika
                  </Link>{' '}
                </>
              )}
              {user && ADMIN_ROLES.includes(user.role) && (
                <>
                  <Link to="/admin/menu" className="auth-account-link">
                    Panel admina
                  </Link>{' '}
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
