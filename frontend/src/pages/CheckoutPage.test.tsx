import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { CartContext, CartProvider, type CartContextValue } from '../cart/CartContext'
import { AuthProvider } from '../auth/AuthContext'
import { CheckoutPage } from './CheckoutPage'

vi.mock('../api/restaurantApi', () => ({
  getRestaurantConfig: vi.fn().mockResolvedValue({
    deliveryFee: { amount: 0, currency: 'PLN' },
    minimumOrderValue: null,
    freeDeliveryThreshold: null,
    isAcceptingOrders: true,
    openingHours: {
      schedule: {
        Sunday: [],
        Monday: [],
        Tuesday: [],
        Wednesday: [],
        Thursday: [],
        Friday: [],
        Saturday: [],
      },
    },
  }),
}))

vi.mock('../api/ordersApi', () => ({
  createOrder: vi.fn().mockResolvedValue({
    orderId: 'order-1',
    number: 'ORD-1',
    guestTrackingToken: 'token-1',
    paymentRedirectUrl: null,
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
  localStorage.clear()
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

  it('navigates to /checkout/confirmation (not back to /cart) after a successful cash/pickup submit', async () => {
    // Regression test: clearing the cart on submit used to flip CheckoutPage's own
    // `items.length === 0` guard to `<Navigate to="/cart" replace />`, which raced the
    // `navigate('/checkout/confirmation')` call and could win, bouncing the customer back to
    // /cart right after they placed the order. See CheckoutPage.tsx's justSubmittedRef.
    localStorage.setItem(
      'pizzashop.cart',
      JSON.stringify([
        {
          menuItemId: 'a',
          menuItemName: 'Test Pizza',
          variantId: null,
          variantName: null,
          extraIds: [],
          extraNames: [],
          unitPriceAmount: 30,
          currency: 'PLN',
          quantity: 1,
          key: 'a',
        },
      ]),
    )

    render(
      <AuthProvider>
        <CartProvider>
          <MemoryRouter initialEntries={['/checkout']}>
            <Routes>
              <Route path="/checkout" element={<CheckoutPage />} />
              <Route path="/checkout/confirmation" element={<p>Confirmation</p>} />
              <Route path="/cart" element={<p>Koszyk</p>} />
            </Routes>
          </MemoryRouter>
        </CartProvider>
      </AuthProvider>,
    )

    fireEvent.click(await screen.findByText('Odbiór osobisty'))
    fireEvent.click(screen.getByText('Dalej'))

    fireEvent.change(screen.getByLabelText('Imię i nazwisko'), { target: { value: 'Jan Kowalski' } })
    fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: '123456789' } })
    fireEvent.click(screen.getByText('Dalej'))

    fireEvent.click(screen.getByText('Dalej')) // fulfillment time: default "now"

    fireEvent.click(screen.getByText('Przy odbiorze / dostawie'))
    fireEvent.click(screen.getByText('Dalej'))

    fireEvent.click(screen.getByText('Dalej')) // promotion: skip

    fireEvent.click(await screen.findByText('Zamawiam'))

    await waitFor(() => {
      expect(screen.queryByText('Confirmation')).not.toBeNull()
    })
    expect(screen.queryByText('Koszyk')).toBeNull()
  })
})
