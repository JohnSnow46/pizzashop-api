import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { CartPage } from './pages/CartPage'
import { CheckoutPage } from './pages/CheckoutPage'
import { MenuPage } from './pages/MenuPage'
import { OrderConfirmationPage } from './pages/OrderConfirmationPage'

/**
 * ADR-0035 shipped catalog + cart; ADR-0036 adds guest checkout (+ its own confirmation route,
 * kept separate from the wizard because it must survive a full page reload on return from
 * PayU). Auth and order tracking are still future iterations — routes for them intentionally
 * not added yet.
 */
export function AppRoutes() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<MenuPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/checkout" element={<CheckoutPage />} />
        <Route path="/checkout/confirmation" element={<OrderConfirmationPage />} />
        {/* TODO: /login, /orders/:trackingToken — przyszłe iteracje */}
      </Routes>
    </Layout>
  )
}
