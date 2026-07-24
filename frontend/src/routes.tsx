import { Route, Routes } from 'react-router-dom'
import { RedirectIfAuthenticated } from './components/auth/RedirectIfAuthenticated'
import { RequireAuth } from './components/auth/RequireAuth'
import { Layout } from './components/Layout'
import { CartPage } from './pages/CartPage'
import { CheckoutPage } from './pages/CheckoutPage'
import { EmployeeOrdersPage } from './pages/EmployeeOrdersPage'
import { LoginPage } from './pages/LoginPage'
import { MenuPage } from './pages/MenuPage'
import { MyAccountPage } from './pages/MyAccountPage'
import { OrderConfirmationPage } from './pages/OrderConfirmationPage'
import { RegisterPage } from './pages/RegisterPage'
import { TrackOrderPage } from './pages/TrackOrderPage'

/**
 * ADR-0035 shipped catalog + cart; ADR-0036 adds guest checkout (+ its own confirmation route,
 * kept separate from the wizard because it must survive a full page reload on return from
 * PayU); ADR-0037 adds customer login/register; ADR-0038 adds SignalR live order tracking and
 * the public `/orders/track/:trackingToken` route below; ADR-0039 adds the `/account` panel
 * (order history + loyalty points), gated behind `RequireAuth`.
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
        <Route path="/orders/track/:trackingToken" element={<TrackOrderPage />} />
        <Route
          path="/account"
          element={
            <RequireAuth>
              <MyAccountPage />
            </RequireAuth>
          }
        />
        <Route
          path="/employee/orders"
          element={
            <RequireAuth roles={['Employee', 'RestaurantAdmin', 'SuperAdmin']}>
              <EmployeeOrdersPage />
            </RequireAuth>
          }
        />
      </Routes>
    </Layout>
  )
}
