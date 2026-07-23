import { createContext, useEffect, useReducer, type ReactNode } from 'react'
import { loadCart, saveCart } from './cartStorage'
import type { CartItem } from './types'

export interface AddCartItemInput {
  menuItemId: string
  menuItemName: string
  variantId: string | null
  variantName: string | null
  extraIds: string[]
  extraNames: string[]
  unitPriceAmount: number
  currency: string
  quantity: number
  /** Precomputed via buildCartItemKey — kept out of the reducer so it stays a pure function of its input. */
  key: string
}

type CartAction =
  | { type: 'add'; item: AddCartItemInput }
  | { type: 'remove'; key: string }
  | { type: 'setQuantity'; key: string; quantity: number }
  | { type: 'clear' }

function cartReducer(items: CartItem[], action: CartAction): CartItem[] {
  switch (action.type) {
    case 'add': {
      const existing = items.find((i) => i.key === action.item.key)
      if (existing) {
        return items.map((i) =>
          i.key === action.item.key ? { ...i, quantity: i.quantity + action.item.quantity } : i,
        )
      }
      return [...items, { ...action.item }]
    }
    case 'remove':
      return items.filter((i) => i.key !== action.key)
    case 'setQuantity': {
      if (action.quantity <= 0) {
        return items.filter((i) => i.key !== action.key)
      }
      return items.map((i) => (i.key === action.key ? { ...i, quantity: action.quantity } : i))
    }
    case 'clear':
      return []
    default:
      return items
  }
}

export interface CartContextValue {
  items: CartItem[]
  totalQuantity: number
  /** Orientation-only total for display — the Api recomputes real pricing at checkout (ADR-0035). */
  totalAmount: number
  addItem: (item: AddCartItemInput) => void
  removeItem: (key: string) => void
  setQuantity: (key: string, quantity: number) => void
  clear: () => void
}

export const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, dispatch] = useReducer(cartReducer, undefined, loadCart)

  useEffect(() => {
    saveCart(items)
  }, [items])

  const totalQuantity = items.reduce((sum, i) => sum + i.quantity, 0)
  const totalAmount = items.reduce((sum, i) => sum + i.unitPriceAmount * i.quantity, 0)

  const value: CartContextValue = {
    items,
    totalQuantity,
    totalAmount,
    addItem: (item) => dispatch({ type: 'add', item }),
    removeItem: (key) => dispatch({ type: 'remove', key }),
    setQuantity: (key, quantity) => dispatch({ type: 'setQuantity', key, quantity }),
    clear: () => dispatch({ type: 'clear' }),
  }

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}
