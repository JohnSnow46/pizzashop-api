import { BrowserRouter } from 'react-router-dom'
import './App.css'
import { AuthProvider } from './auth/AuthContext'
import { CartProvider } from './cart/CartContext'
import { AppRoutes } from './routes'

function App() {
  return (
    <AuthProvider>
      <CartProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </CartProvider>
    </AuthProvider>
  )
}

export default App
