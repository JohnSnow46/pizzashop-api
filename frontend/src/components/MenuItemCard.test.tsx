import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
import { CartProvider } from '../cart/CartContext'
import { useCart } from '../hooks/useCart'
import { MenuItemCard } from './MenuItemCard'
import type { MenuItem } from '../api/types'

function makeItem(overrides: Partial<MenuItem> = {}): MenuItem {
  return {
    id: 'pizza-1',
    name: 'Margherita',
    description: 'Klasyczna pizza z sosem pomidorowym i mozzarellą',
    category: 'Pizza',
    basePrice: { amount: 25, currency: 'PLN' },
    isAvailable: true,
    imageUrl: null,
    variants: [],
    baseIngredients: [],
    allowedExtras: [],
    ...overrides,
  }
}

/** Sibling consumer that exposes the cart's items for assertions, without reaching into cartStorage internals. */
function CartItemsProbe() {
  const { items } = useCart()
  return (
    <ul data-testid="cart-items">
      {items.map((item) => (
        <li key={item.key} data-testid="cart-item">
          {JSON.stringify(item)}
        </li>
      ))}
    </ul>
  )
}

function renderCard(item: MenuItem) {
  return render(
    <CartProvider>
      <MenuItemCard item={item} />
      <CartItemsProbe />
    </CartProvider>,
  )
}

describe('MenuItemCard', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    cleanup()
  })

  it('with no variants and no extras selected, displays basePrice', () => {
    const item = makeItem({ basePrice: { amount: 25, currency: 'PLN' } })
    renderCard(item)

    expect(screen.getByText('25.00 PLN')).not.toBeNull()
  })

  it('selecting a non-default variant replaces basePrice with the variant price (not additive)', () => {
    const item = makeItem({
      basePrice: { amount: 25, currency: 'PLN' },
      variants: [
        { id: 'v-small', name: 'Mała', price: { amount: 20, currency: 'PLN' }, isDefault: true },
        { id: 'v-large', name: 'Duża', price: { amount: 35, currency: 'PLN' }, isDefault: false },
      ],
    })
    renderCard(item)

    fireEvent.click(screen.getByLabelText(/Duża/))

    expect(screen.getByText('35.00 PLN')).not.toBeNull()
    // Explicitly guard against a regression to additive pricing (basePrice + variantPrice).
    expect(screen.queryByText('60.00 PLN')).toBeNull()
  })

  it('with a variant selected and extras checked, displays variant price + sum of extras', () => {
    const item = makeItem({
      basePrice: { amount: 25, currency: 'PLN' },
      variants: [
        { id: 'v-small', name: 'Mała', price: { amount: 20, currency: 'PLN' }, isDefault: true },
        { id: 'v-large', name: 'Duża', price: { amount: 35, currency: 'PLN' }, isDefault: false },
      ],
      allowedExtras: [
        { id: 'e-cheese', name: 'Ser', extraPrice: { amount: 5, currency: 'PLN' }, isAvailable: true, category: null },
        { id: 'e-olives', name: 'Oliwki', extraPrice: { amount: 3, currency: 'PLN' }, isAvailable: true, category: null },
      ],
    })
    renderCard(item)

    fireEvent.click(screen.getByLabelText(/Duża/))
    fireEvent.click(screen.getByLabelText(/Ser/))
    fireEvent.click(screen.getByLabelText(/Oliwki/))

    // variant (35) + cheese (5) + olives (3) = 43
    expect(screen.getByText('43.00 PLN')).not.toBeNull()
  })

  it('clicking "Dodaj do koszyka" adds the item to the cart with the correct unitPriceAmount', () => {
    const item = makeItem({
      basePrice: { amount: 25, currency: 'PLN' },
      variants: [
        { id: 'v-small', name: 'Mała', price: { amount: 20, currency: 'PLN' }, isDefault: true },
        { id: 'v-large', name: 'Duża', price: { amount: 35, currency: 'PLN' }, isDefault: false },
      ],
      allowedExtras: [
        { id: 'e-cheese', name: 'Ser', extraPrice: { amount: 5, currency: 'PLN' }, isAvailable: true, category: null },
      ],
    })
    renderCard(item)

    fireEvent.click(screen.getByLabelText(/Duża/))
    fireEvent.click(screen.getByLabelText(/Ser/))
    fireEvent.click(screen.getByRole('button', { name: 'Dodaj do koszyka' }))

    const cartItems = screen.getAllByTestId('cart-item')
    expect(cartItems).toHaveLength(1)
    const addedItem = JSON.parse(cartItems[0].textContent ?? '{}')
    expect(addedItem.menuItemId).toBe('pizza-1')
    expect(addedItem.variantId).toBe('v-large')
    expect(addedItem.extraIds).toEqual(['e-cheese'])
    // variant (35) + cheese (5) = 40
    expect(addedItem.unitPriceAmount).toBe(40)
  })
})
