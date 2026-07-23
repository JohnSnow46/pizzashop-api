import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useCart } from '../hooks/useCart'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  const { totalQuantity } = useCart()

  return (
    <div className="layout">
      <header className="header">
        <h1>
          <Link to="/">PizzaShop</Link>
        </h1>
        <Link to="/cart" className="cart-link">
          Koszyk ({totalQuantity})
        </Link>
      </header>
      <main>{children}</main>
    </div>
  )
}
