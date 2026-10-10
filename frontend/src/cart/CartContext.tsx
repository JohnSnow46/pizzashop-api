import { createContext, useContext, useEffect, useRef, useReducer, type ReactNode } from 'react'
import { AuthContext } from '../auth/AuthContext'
import { addCartItem, clearCart as clearCartApi, getMyCart, removeCartItem, updateCartItemQuantity } from '../api/cartApi'
import { getMenu } from '../api/menuApi'
import type { CartDto, MenuItem } from '../api/types'
import { loadCart, saveCart } from './cartStorage'
import { buildCartItemKey, type CartItem } from './types'

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
  | { type: 'replaceAll'; items: CartItem[] }

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
    case 'replaceAll':
      return action.items
    default:
      return items
  }
}

/**
 * Resolves a persistent cart (ADR-0043, raw ids only) against the live menu catalog into
 * display-ready CartItems — the backend deliberately stores no name/price snapshot (a cart
 * is a draft, not a transaction), so this mirrors what MenuItemCard already computes when
 * adding an item. A referenced menu item/variant/extra that no longer exists in the catalog
 * (e.g. deleted since being added) is skipped rather than crashing the cart view.
 */
function hydrateFromServerCart(cart: CartDto, menu: MenuItem[]): { item: CartItem; backendId: string }[] {
  const menuById = new Map(menu.map((m) => [m.id, m]))

  const resolved: { item: CartItem; backendId: string }[] = []
  for (const line of cart.items) {
    const menuItem = menuById.get(line.menuItemId)
    if (!menuItem) {
      continue
    }

    const variant = line.variantId ? (menuItem.variants.find((v) => v.id === line.variantId) ?? null) : null
    const extras = menuItem.allowedExtras.filter((extra) => line.extraIds.includes(extra.id))
    const unitPriceAmount =
      (variant?.price.amount ?? menuItem.basePrice.amount) + extras.reduce((sum, extra) => sum + extra.extraPrice.amount, 0)

    resolved.push({
      backendId: line.id,
      item: {
        key: buildCartItemKey(line.menuItemId, line.variantId, line.extraIds),
        menuItemId: line.menuItemId,
        menuItemName: menuItem.name,
        variantId: line.variantId,
        variantName: variant?.name ?? null,
        extraIds: line.extraIds,
        extraNames: extras.map((extra) => extra.name),
        unitPriceAmount,
        currency: variant?.price.currency ?? menuItem.basePrice.currency,
        quantity: line.quantity,
      },
    })
  }
  return resolved
}

/**
 * Merges the guest's localStorage cart into the server-backed one on login (roadmap #9,
 * left as a separate decision by ADR-0043). Decision made here: sum quantities on a
 * matching selection, by simply replaying each guest line through addCartItem -- the
 * backend already merges into an existing line with the same (menuItemId, variantId,
 * extraIds) and sums quantity (ADR-0043 SS B), so this needs no client-side merge logic of
 * its own. A line is only dropped from localStorage once its own POST has succeeded, so a
 * partial failure (one line fails, others succeed) leaves just the unmerged remainder for
 * the next mount to retry rather than silently losing it or double-merging what already
 * went through.
 */
async function mergeGuestCartIntoServerCart(): Promise<void> {
  const guestItems = loadCart()
  if (guestItems.length === 0) {
    return
  }

  const remaining: CartItem[] = []
  for (const guestItem of guestItems) {
    try {
      await addCartItem({
        menuItemId: guestItem.menuItemId,
        variantId: guestItem.variantId,
        extraIds: guestItem.extraIds,
        quantity: guestItem.quantity,
        notes: null,
      })
    } catch (error) {
      console.error('Failed to merge a guest cart line into the server-side cart.', error)
      remaining.push(guestItem)
    }
  }
  saveCart(remaining)
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

/**
 * `useContext` directly (not the throwing `useAuth()` wrapper) — `CartProvider` must stay
 * usable standalone (CartContext.test.tsx renders it with no AuthProvider above it), and
 * "no auth context" is correctly treated the same as "not logged in": the existing
 * client-side-only cart, unchanged. Logged-in customers only (`isAuthenticated` plus a
 * `customerId` — Employee/Admin accounts have neither a cart nor a reason to use one).
 */
function useIsBackendCartCustomer(): boolean {
  const auth = useContext(AuthContext)
  return Boolean(auth?.isAuthenticated && auth.user?.customerId)
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, dispatch] = useReducer(cartReducer, undefined, loadCart)
  const useBackendCart = useIsBackendCartCustomer()

  // Backend CartItem id per line key — populated by hydration and by addItem's response.
  // A plain ref (not state): it's bookkeeping for which network call to fire next, not
  // something that should itself trigger a re-render.
  const backendIdByKey = useRef<Map<string, string>>(new Map())
  // In-flight addItem calls, so a remove/setQuantity on a line added moments ago waits for
  // its real backend id instead of acting on nothing.
  const pendingAddByKey = useRef<Map<string, Promise<string>>>(new Map())

  async function resolveBackendId(key: string): Promise<string | undefined> {
    const known = backendIdByKey.current.get(key)
    if (known) {
      return known
    }
    return pendingAddByKey.current.get(key)
  }

  // Persist only the guest cart -- the backend is the source of truth once
  // useBackendCart is true, and must not be clobbered by a stale localStorage write.
  useEffect(() => {
    if (!useBackendCart) {
      saveCart(items)
    }
  }, [items, useBackendCart])

  // Switches the displayed cart's source on every auth-mode transition (including the
  // initial mount): the server cart + live catalog when logged in as a customer, the
  // existing localStorage snapshot otherwise.
  useEffect(() => {
    let cancelled = false

    if (!useBackendCart) {
      dispatch({ type: 'replaceAll', items: loadCart() })
      return
    }

    backendIdByKey.current = new Map()
    pendingAddByKey.current = new Map()

    mergeGuestCartIntoServerCart()
      .then(() => Promise.all([getMyCart(), getMenu()]))
      .then(([cart, menu]) => {
        if (cancelled) {
          return
        }
        const resolved = hydrateFromServerCart(cart, menu)
        backendIdByKey.current = new Map(resolved.map(({ item, backendId }) => [item.key, backendId]))
        dispatch({ type: 'replaceAll', items: resolved.map((r) => r.item) })
      })
      .catch((error: unknown) => {
        // Best-effort: leaves the cart empty rather than blocking the rest of the app on
        // a failed fetch. The next successful mutation/hydration will self-correct.
        console.error('Failed to load the server-side cart.', error)
      })

    return () => {
      cancelled = true
    }
  }, [useBackendCart])

  const totalQuantity = items.reduce((sum, i) => sum + i.quantity, 0)
  const totalAmount = items.reduce((sum, i) => sum + i.unitPriceAmount * i.quantity, 0)

  function addItem(item: AddCartItemInput) {
    dispatch({ type: 'add', item })

    if (!useBackendCart) {
      return
    }

    const promise = addCartItem({
      menuItemId: item.menuItemId,
      variantId: item.variantId,
      extraIds: item.extraIds,
      quantity: item.quantity,
      notes: null,
    })
      .then((id) => {
        backendIdByKey.current.set(item.key, id)
        return id
      })
      .catch((error: unknown) => {
        console.error('Failed to add the item to the server-side cart.', error)
        throw error
      })
      .finally(() => {
        pendingAddByKey.current.delete(item.key)
      })

    // A failed add has no other consumer unless a remove/setQuantity happens to await it
    // via resolveBackendId before it settles -- attach an inert catch so that common case
    // doesn't surface as an unhandled rejection; the error is already logged above, and any
    // real awaiter still observes the same rejection independently.
    promise.catch(() => {})

    pendingAddByKey.current.set(item.key, promise)
  }

  function removeItem(key: string) {
    dispatch({ type: 'remove', key })

    if (!useBackendCart) {
      return
    }

    void resolveBackendId(key)
      .then((id) => {
        if (!id) {
          return
        }
        backendIdByKey.current.delete(key)
        return removeCartItem(id)
      })
      .catch((error: unknown) => console.error('Failed to remove the item from the server-side cart.', error))
  }

  function setQuantity(key: string, quantity: number) {
    dispatch({ type: 'setQuantity', key, quantity })

    if (!useBackendCart) {
      return
    }

    void resolveBackendId(key)
      .then((id) => {
        if (!id) {
          return
        }
        if (quantity <= 0) {
          backendIdByKey.current.delete(key)
          return removeCartItem(id)
        }
        return updateCartItemQuantity(id, quantity)
      })
      .catch((error: unknown) => console.error('Failed to update the item in the server-side cart.', error))
  }

  function clear() {
    dispatch({ type: 'clear' })

    if (!useBackendCart) {
      return
    }

    backendIdByKey.current = new Map()
    pendingAddByKey.current = new Map()
    void clearCartApi().catch((error: unknown) => console.error('Failed to clear the server-side cart.', error))
  }

  const value: CartContextValue = {
    items,
    totalQuantity,
    totalAmount,
    addItem,
    removeItem,
    setQuantity,
    clear,
  }

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}
