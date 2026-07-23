import { useContext } from 'react'
import { CartContext, type CartContextValue } from '../cart/CartContext'

/** Access to the cart state/actions from CartProvider (must be rendered above the caller). */
export function useCart(): CartContextValue {
  const context = useContext(CartContext)
  if (!context) {
    throw new Error('useCart must be used within a CartProvider')
  }
  return context
}
