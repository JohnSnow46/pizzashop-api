import { describe, it, expect, afterEach, beforeEach } from 'vitest'
import { render, screen, cleanup, fireEvent } from '@testing-library/react'
import { CartProvider } from '../cart/CartContext'
import { MenuList } from './MenuList'
import type { Ingredient, MenuItem } from '../api/types'

function ingredient(name: string): Ingredient {
  return { id: name, name, extraPrice: { amount: 0, currency: 'PLN' }, isAvailable: true, category: null }
}

function makeItem(overrides: Partial<MenuItem> = {}): MenuItem {
  return {
    id: 'pizza-1',
    name: 'Margherita',
    description: null,
    category: 'Pizza',
    basePrice: { amount: 25, currency: 'PLN' },
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

describe('MenuList', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    cleanup()
  })

  it('finds items by a base ingredient, not just by name', () => {
    render(
      <CartProvider>
        <MenuList
          items={[
            makeItem({ id: 'p1', name: 'Margherita', baseIngredients: [ingredient('mozzarella')] }),
            makeItem({ id: 'p2', name: 'Funghi', baseIngredients: [ingredient('mozzarella'), ingredient('pieczarki')] }),
          ]}
        />
      </CartProvider>,
    )

    fireEvent.change(screen.getByLabelText('Szukaj w menu'), { target: { value: 'Pieczar' } })

    expect(screen.queryByText('Margherita')).toBeNull()
    expect(screen.getByText('Funghi')).not.toBeNull()
  })
})
