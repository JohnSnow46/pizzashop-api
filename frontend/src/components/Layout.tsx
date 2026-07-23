import type { ReactNode } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { useCart } from '../hooks/useCart'

interface LayoutProps {
  children: ReactNode
}

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
