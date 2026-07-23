import { Route, Routes } from 'react-router-dom'
import { RedirectIfAuthenticated } from './components/auth/RedirectIfAuthenticated'
import { Layout } from './components/Layout'
import { CartPage } from './pages/CartPage'
import { CheckoutPage } from './pages/CheckoutPage'
import { LoginPage } from './pages/LoginPage'
import { MenuPage } from './pages/MenuPage'
import { OrderConfirmationPage } from './pages/OrderConfirmationPage'
import { RegisterPage } from './pages/RegisterPage'

/**
 * ADR-0035 shipped catalog + cart; ADR-0036 adds guest checkout (+ its own confirmation route,
 * kept separate from the wizard because it must survive a full page reload on return from
 * PayU); ADR-0037 adds customer login/register. Order tracking is still a future iteration.
 */
export function AppRoutes() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<MenuPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/checkout" element={<CheckoutPage />} />
        <Route path="/checkout/confirmation" element={<OrderConfirmationPage />} />
        <Route
          path="/login"
          element={
            <RedirectIfAuthenticated>
              <LoginPage />
            </RedirectIfAuthenticated>
          }
        />
        <Route
          path="/register"
          element={
            <RedirectIfAuthenticated>
              <RegisterPage />
            </RedirectIfAuthenticated>
          }
        />
        {/* TODO: /orders/:trackingToken — przyszła iteracja */}
      </Routes>
    </Layout>
  )
}
