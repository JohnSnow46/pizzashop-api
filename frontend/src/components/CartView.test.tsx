import { describe, it, expect, afterEach, vi } from 'vitest'
import { render, screen, cleanup, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { CartView } from './CartView'

const clear = vi.fn()

vi.mock('../hooks/useCart', () => ({
  useCart: () => ({
    items: [
      {
        key: 'k1',
        menuItemName: 'Margherita',
        variantName: null,
        extraNames: [],
        unitPriceAmount: 30,
        currency: 'PLN',
        quantity: 1,
      },
    ],
    totalAmount: 30,
    setQuantity: vi.fn(),
    removeItem: vi.fn(),
    clear,
  }),
}))

function renderCart() {
  render(
    <MemoryRouter>
      <CartView />
    </MemoryRouter>,
  )
}

describe('CartView', () => {
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
    clear.mockClear()
  })

  it('keeps the cart when clearing is not confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    renderCart()

    fireEvent.click(screen.getByRole('button', { name: 'Wyczyść koszyk' }))

    expect(clear).not.toHaveBeenCalled()
  })

  it('clears the cart once confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    renderCart()

    fireEvent.click(screen.getByRole('button', { name: 'Wyczyść koszyk' }))

    expect(clear).toHaveBeenCalledOnce()
  })
})
