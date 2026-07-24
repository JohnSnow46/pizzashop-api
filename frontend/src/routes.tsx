import { Route, Routes } from 'react-router-dom'
import { RedirectIfAuthenticated } from './components/auth/RedirectIfAuthenticated'
import { RequireAuth } from './components/auth/RequireAuth'
import { Layout } from './components/Layout'
import { AdminMenuPage } from './pages/AdminMenuPage'
import { AdminPromotionsPage } from './pages/AdminPromotionsPage'
import { AdminRestaurantPage } from './pages/AdminRestaurantPage'
import { AdminStaffPage } from './pages/AdminStaffPage'
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
 * (order history + loyalty points), gated behind `RequireAuth`. `/admin/menu` is the admin
 * catalog management page (menu items + ingredients), gated to `AuthRoles.Admin` equivalents.
 * `/admin/promotions` is the admin promotion management page (list + create/edit, ADR-0019),
 * gated the same way. `/admin/restaurant` is the admin restaurant configuration page (opening
 * hours, delivery area, ordering/free-delivery thresholds), gated the same way. `/admin/staff`
 * is the admin staff account management page (list + create via RegisterStaffAccountCommand),
 * gated the same way.
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
        <Route
          path="/admin/menu"
          element={
            <RequireAuth roles={['RestaurantAdmin', 'SuperAdmin']}>
              <AdminMenuPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/promotions"
          element={
            <RequireAuth roles={['RestaurantAdmin', 'SuperAdmin']}>
              <AdminPromotionsPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/restaurant"
          element={
            <RequireAuth roles={['RestaurantAdmin', 'SuperAdmin']}>
              <AdminRestaurantPage />
            </RequireAuth>
          }
        />
        <Route
          path="/admin/staff"
          element={
            <RequireAuth roles={['RestaurantAdmin', 'SuperAdmin']}>
              <AdminStaffPage />
            </RequireAuth>
          }
        />
      </Routes>
    </Layout>
  )
}
