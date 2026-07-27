import { describe, it, expect, beforeEach } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import { CartProvider } from './CartContext'
import { useCart } from '../hooks/useCart'
import { buildCartItemKey } from './types'
import type { AddCartItemInput } from './CartContext'

function makeItem(overrides: Partial<AddCartItemInput> = {}): AddCartItemInput {
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

describe('CartContext', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('starts with empty state', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    expect(result.current.items).toEqual([])
    expect(result.current.totalQuantity).toBe(0)
    expect(result.current.totalAmount).toBe(0)
  })

  it('addItem adds a new line', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem()))
    expect(result.current.items).toHaveLength(1)
    expect(result.current.items[0].quantity).toBe(1)
  })

  it('addItem with the same key increments quantity instead of duplicating', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem({ quantity: 1 })))
    act(() => result.current.addItem(makeItem({ quantity: 2 })))
    expect(result.current.items).toHaveLength(1)
    expect(result.current.items[0].quantity).toBe(3)
  })

  it('computes totalQuantity and totalAmount across multiple distinct lines', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-1', unitPriceAmount: 30, quantity: 2 })))
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-2', unitPriceAmount: 25, quantity: 1 })))
    expect(result.current.totalQuantity).toBe(3)
    expect(result.current.totalAmount).toBe(30 * 2 + 25 * 1)
  })

  it('removeItem removes only the matching key', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-1' })))
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-2' })))
    const keyToRemove = buildCartItemKey('pizza-1', null, [])
    act(() => result.current.removeItem(keyToRemove))
    expect(result.current.items).toHaveLength(1)
    expect(result.current.items[0].menuItemId).toBe('pizza-2')
  })

  it('setQuantity updates the quantity when positive', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem()))
    const key = buildCartItemKey('pizza-1', null, [])
    act(() => result.current.setQuantity(key, 5))
    expect(result.current.items[0].quantity).toBe(5)
  })

  it('setQuantity to 0 removes the line', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem()))
    const key = buildCartItemKey('pizza-1', null, [])
    act(() => result.current.setQuantity(key, 0))
    expect(result.current.items).toHaveLength(0)
  })

  it('setQuantity to a negative number removes the line', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem()))
    const key = buildCartItemKey('pizza-1', null, [])
    act(() => result.current.setQuantity(key, -1))
    expect(result.current.items).toHaveLength(0)
  })

  it('clear empties all items', () => {
    const { result } = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-1' })))
    act(() => result.current.addItem(makeItem({ menuItemId: 'pizza-2' })))
    act(() => result.current.clear())
    expect(result.current.items).toEqual([])
  })

  it('persists to localStorage and a fresh CartProvider picks up the saved state', () => {
    const first = renderHook(() => useCart(), { wrapper: CartProvider })
    act(() => first.result.current.addItem(makeItem({ menuItemId: 'pizza-1', quantity: 2 })))

    const second = renderHook(() => useCart(), { wrapper: CartProvider })
    expect(second.result.current.items).toHaveLength(1)
    expect(second.result.current.items[0].menuItemId).toBe('pizza-1')
    expect(second.result.current.items[0].quantity).toBe(2)
  })
})
