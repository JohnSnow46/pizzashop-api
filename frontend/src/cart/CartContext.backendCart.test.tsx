import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { act, cleanup, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { AuthContext, type AuthContextValue } from '../auth/AuthContext'
import { CartProvider, type AddCartItemInput } from './CartContext'
import { useCart } from '../hooks/useCart'
import { buildCartItemKey } from './types'
import type { MenuItem } from '../api/types'

vi.mock('../api/cartApi', () => ({
  getMyCart: vi.fn(),
  addCartItem: vi.fn(),
  updateCartItemQuantity: vi.fn(),
  removeCartItem: vi.fn(),
  clearCart: vi.fn(),
}))
vi.mock('../api/menuApi', () => ({
  getMenu: vi.fn(),
}))

import { getMyCart, addCartItem, updateCartItemQuantity, removeCartItem, clearCart } from '../api/cartApi'
import { getMenu } from '../api/menuApi'

const getMyCartMock = vi.mocked(getMyCart)
const addCartItemMock = vi.mocked(addCartItem)
const updateCartItemQuantityMock = vi.mocked(updateCartItemQuantity)
const removeCartItemMock = vi.mocked(removeCartItem)
const clearCartMock = vi.mocked(clearCart)
const getMenuMock = vi.mocked(getMenu)

const loggedInCustomer: AuthContextValue = {
  user: { userAccountId: 'u-1', email: 'jan@example.com', role: 'Customer', customerId: 'cust-1', fullName: 'Jan' },
  token: 'token-1',
  isAuthenticated: true,
  isLoading: false,
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

function wrapper({ children }: { children: ReactNode }) {
  return (
    <AuthContext.Provider value={loggedInCustomer}>
      <CartProvider>{children}</CartProvider>
    </AuthContext.Provider>
  )
}

function makeMenuItem(overrides: Partial<MenuItem> = {}): MenuItem {
  return {
    id: 'pizza-1',
    name: 'Margherita',
    description: null,
    category: 'Pizza',
    basePrice: { amount: 30, currency: 'PLN' },
    isAvailable: true,
    imageUrl: null,
    variants: [],
    baseIngredients: [],
    allowedExtras: [],
    averageRating: null,
    reviewCount: 0,
    ...overrides,
  }
}

function makeAddInput(overrides: Partial<AddCartItemInput> = {}): AddCartItemInput {
  const menuItemId = overrides.menuItemId ?? 'pizza-1'
  const variantId = overrides.variantId ?? null
  const extraIds = overrides.extraIds ?? []
  return {
    menuItemId,
    menuItemName: 'Margherita',
    variantId,
    variantName: null,
    extraIds,
    extraNames: [],
    unitPriceAmount: 30,
    currency: 'PLN',
    quantity: 1,
    key: buildCartItemKey(menuItemId, variantId, extraIds),
    ...overrides,
  }
}

beforeEach(() => {
  getMyCartMock.mockReset().mockResolvedValue({ items: [], updatedAt: '2026-01-01T00:00:00Z' })
  getMenuMock.mockReset().mockResolvedValue([])
  addCartItemMock.mockReset()
  updateCartItemQuantityMock.mockReset().mockResolvedValue(undefined)
  removeCartItemMock.mockReset().mockResolvedValue(undefined)
  clearCartMock.mockReset().mockResolvedValue(undefined)
})

afterEach(() => cleanup())

describe('CartContext (logged-in customer, server-backed cart)', () => {
  it('hydrates items from the server cart resolved against the live menu catalog', async () => {
    getMyCartMock.mockResolvedValue({
      items: [{ id: 'line-1', menuItemId: 'pizza-1', variantId: null, extraIds: [], quantity: 2, notes: null }],
      updatedAt: '2026-01-01T00:00:00Z',
    })
    getMenuMock.mockResolvedValue([makeMenuItem()])

    const { result } = renderHook(() => useCart(), { wrapper })

    await waitFor(() => expect(result.current.items).toHaveLength(1))
    expect(result.current.items[0]).toMatchObject({
      menuItemId: 'pizza-1',
      menuItemName: 'Margherita',
      quantity: 2,
      unitPriceAmount: 30,
    })
  })

  it('skips a cart line whose menu item no longer exists in the catalog', async () => {
    getMyCartMock.mockResolvedValue({
      items: [{ id: 'line-1', menuItemId: 'deleted-item', variantId: null, extraIds: [], quantity: 1, notes: null }],
      updatedAt: '2026-01-01T00:00:00Z',
    })
    getMenuMock.mockResolvedValue([])

    const { result } = renderHook(() => useCart(), { wrapper })

    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())
    expect(result.current.items).toHaveLength(0)
  })

  it('addItem posts to the server cart and reflects the change optimistically', async () => {
    addCartItemMock.mockResolvedValue('backend-id-1')
    const { result } = renderHook(() => useCart(), { wrapper })
    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())

    act(() => result.current.addItem(makeAddInput()))

    expect(result.current.items).toHaveLength(1)
    await waitFor(() =>
      expect(addCartItemMock).toHaveBeenCalledWith({
        menuItemId: 'pizza-1',
        variantId: null,
        extraIds: [],
        quantity: 1,
        notes: null,
      }),
    )
  })

  it('removeItem right after addItem waits for the in-flight add before calling DELETE', async () => {
    addCartItemMock.mockResolvedValue('backend-id-1')
    const { result } = renderHook(() => useCart(), { wrapper })
    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())

    const item = makeAddInput()
    act(() => {
      result.current.addItem(item)
      result.current.removeItem(item.key)
    })

    expect(result.current.items).toHaveLength(0)
    await waitFor(() => expect(removeCartItemMock).toHaveBeenCalledWith('backend-id-1'))
  })

  it('setQuantity to 0 calls DELETE, not PATCH', async () => {
    addCartItemMock.mockResolvedValue('backend-id-2')
    const { result } = renderHook(() => useCart(), { wrapper })
    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())

    const item = makeAddInput()
    act(() => {
      result.current.addItem(item)
      result.current.setQuantity(item.key, 0)
    })

    await waitFor(() => expect(removeCartItemMock).toHaveBeenCalledWith('backend-id-2'))
    expect(updateCartItemQuantityMock).not.toHaveBeenCalled()
  })

  it('setQuantity to a positive number calls PATCH with the new quantity', async () => {
    addCartItemMock.mockResolvedValue('backend-id-3')
    const { result } = renderHook(() => useCart(), { wrapper })
    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())

    const item = makeAddInput()
    act(() => {
      result.current.addItem(item)
      result.current.setQuantity(item.key, 5)
    })

    await waitFor(() => expect(updateCartItemQuantityMock).toHaveBeenCalledWith('backend-id-3', 5))
    expect(removeCartItemMock).not.toHaveBeenCalled()
  })

  it('clear calls the server-side clear endpoint', async () => {
    const { result } = renderHook(() => useCart(), { wrapper })
    await waitFor(() => expect(getMyCartMock).toHaveBeenCalled())

    act(() => result.current.clear())

    await waitFor(() => expect(clearCartMock).toHaveBeenCalled())
  })
})
