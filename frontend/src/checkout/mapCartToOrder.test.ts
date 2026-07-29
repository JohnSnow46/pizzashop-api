import { describe, it, expect } from 'vitest'
import { cartItemsToOrderItems, cartItemsToPromotionLines } from './mapCartToOrder'
import type { CartItem } from '../cart/types'

const items: CartItem[] = [
  {
    key: 'margherita|variant-1|extra-1',
    menuItemId: 'margherita',
    menuItemName: 'Margherita',
    variantId: 'variant-1',
    variantName: 'Large',
    extraIds: ['extra-1'],
    extraNames: ['Extra cheese'],
    unitPriceAmount: 32.5,
    currency: 'PLN',
    quantity: 2,
  },
]

describe('cartItemsToOrderItems', () => {
  it('maps cart lines onto CreateOrderItem fields', () => {
    const result = cartItemsToOrderItems(items)
    expect(result).toEqual([
      {
        menuItemId: 'margherita',
        variantId: 'variant-1',
        quantity: 2,
        extraIngredientIds: ['extra-1'],
        notes: null,
      },
    ])
  })

  it('does not forward price data from the cart', () => {
    const result = cartItemsToOrderItems(items)
    expect(result[0]).not.toHaveProperty('unitPriceAmount')
    expect(result[0]).not.toHaveProperty('price')
    expect(result[0]).not.toHaveProperty('amount')
    expect(result[0]).not.toHaveProperty('currency')
  })

  it('returns an empty array for an empty list', () => {
    expect(cartItemsToOrderItems([])).toEqual([])
  })
})

describe('cartItemsToPromotionLines', () => {
  it('builds unitPrice from unitPriceAmount and currency', () => {
    const result = cartItemsToPromotionLines(items)
    expect(result).toEqual([
      {
        menuItemId: 'margherita',
        unitPrice: { amount: 32.5, currency: 'PLN' },
        quantity: 2,
      },
    ])
  })

  it('returns an empty array for an empty list', () => {
    expect(cartItemsToPromotionLines([])).toEqual([])
  })
})
