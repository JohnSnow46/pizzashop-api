import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { CartContext, type CartContextValue } from '../cart/CartContext'
import { CheckoutPage } from './CheckoutPage'

vi.mock('../api/restaurantApi', () => ({
  getRestaurantConfig: vi.fn().mockResolvedValue({
    deliveryFee: { amount: 0, currency: 'PLN' },
  }),
}))

const emptyCart: CartContextValue = {
  items: [],
  totalQuantity: 0,
  totalAmount: 0,
  addItem: vi.fn(),
  removeItem: vi.fn(),
  setQuantity: vi.fn(),
  clear: vi.fn(),
}

afterEach(() => {
  cleanup()
})

describe('CheckoutPage', () => {
  it('redirects to /cart when entered directly with an empty cart', async () => {
    render(
      <CartContext.Provider value={emptyCart}>
        <MemoryRouter initialEntries={['/checkout']}>
          <Routes>
            <Route path="/checkout" element={<CheckoutPage />} />
            <Route path="/cart" element={<p>Koszyk</p>} />
          </Routes>
        </MemoryRouter>
      </CartContext.Provider>,
    )

    expect(await screen.findByText('Koszyk')).toBeDefined()
  })
})
