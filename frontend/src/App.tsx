import { BrowserRouter } from 'react-router-dom'
import './App.css'
import { CartProvider } from './cart/CartContext'
import { AppRoutes } from './routes'

function App() {
  return (
    <CartProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </CartProvider>
  )
}

export default App
